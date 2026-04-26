using System.Buffers.Binary;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WhipCursor;

public sealed record WhipAnimation(
    int Width,
    int Height,
    TimeSpan FrameDuration,
    IReadOnlyList<BitmapSource> Frames);

public static class QuickTimeAnimationDecoder
{
    public static WhipAnimation Load(string path)
    {
        var data = File.ReadAllBytes(path);
        var movie = FindChild(data, 0, data.Length, "moov")
            ?? throw new InvalidDataException("The MOV file does not contain a moov atom.");

        foreach (var track in Children(data, movie.DataOffset, movie.EndOffset).Where(atom => atom.Type == "trak"))
        {
            var animation = TryLoadTrack(data, track);
            if (animation is not null)
            {
                return animation;
            }
        }

        throw new InvalidDataException("No QuickTime Animation video track was found.");
    }

    private static WhipAnimation? TryLoadTrack(byte[] data, Atom track)
    {
        var mdia = FindChild(data, track.DataOffset, track.EndOffset, "mdia");
        if (mdia is null)
        {
            return null;
        }

        var handler = FindChild(data, mdia.Value.DataOffset, mdia.Value.EndOffset, "hdlr");
        if (handler is null || ReadAscii(data, handler.Value.DataOffset + 8, 4) != "vide")
        {
            return null;
        }

        var minf = FindChild(data, mdia.Value.DataOffset, mdia.Value.EndOffset, "minf");
        var stbl = minf is null ? null : FindChild(data, minf.Value.DataOffset, minf.Value.EndOffset, "stbl");
        if (stbl is null)
        {
            return null;
        }

        var stsd = RequireChild(data, stbl.Value, "stsd");
        var entryOffset = stsd.DataOffset + 8;
        var codec = ReadAscii(data, entryOffset + 4, 4);
        if (codec != "rle ")
        {
            return null;
        }

        var width = ReadUInt16(data, entryOffset + 32);
        var height = ReadUInt16(data, entryOffset + 34);
        var depth = ReadUInt16(data, entryOffset + 82);
        if (depth != 32)
        {
            throw new NotSupportedException($"Only 32-bit QuickTime Animation clips are supported. This clip is {depth}-bit.");
        }

        var mdhd = RequireChild(data, mdia.Value, "mdhd");
        var timescale = ReadUInt32(data, mdhd.DataOffset + 12);
        var frameDuration = ReadFrameDuration(data, RequireChild(data, stbl.Value, "stts"), timescale);

        var sampleSizes = ReadSampleSizes(data, RequireChild(data, stbl.Value, "stsz"));
        var chunkOffsets = ReadChunkOffsets(data, stbl.Value);
        var sampleToChunks = ReadSampleToChunks(data, RequireChild(data, stbl.Value, "stsc"));
        var sampleRanges = BuildSampleRanges(sampleSizes, chunkOffsets, sampleToChunks);

        var argbFrame = new byte[width * height * 4];
        var frames = new List<BitmapSource>(sampleSizes.Count);

        foreach (var range in sampleRanges)
        {
            DecodeFrame(data.AsSpan(range.Offset, range.Size), argbFrame, width, height);
            frames.Add(CreateBitmap(argbFrame, width, height));
        }

        return new WhipAnimation(width, height, frameDuration, frames);
    }

    private static TimeSpan ReadFrameDuration(byte[] data, Atom stts, uint timescale)
    {
        if (timescale == 0)
        {
            return TimeSpan.FromMilliseconds(33);
        }

        var entryCount = ReadUInt32(data, stts.DataOffset + 4);
        if (entryCount == 0)
        {
            return TimeSpan.FromMilliseconds(33);
        }

        var duration = ReadUInt32(data, stts.DataOffset + 12);
        var milliseconds = Math.Clamp(duration * 1000.0 / timescale, 16.0, 250.0);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static List<int> ReadSampleSizes(byte[] data, Atom stsz)
    {
        var sampleSize = ReadUInt32(data, stsz.DataOffset + 4);
        var sampleCount = checked((int)ReadUInt32(data, stsz.DataOffset + 8));
        var sizes = new List<int>(sampleCount);

        if (sampleSize != 0)
        {
            for (var index = 0; index < sampleCount; index++)
            {
                sizes.Add(checked((int)sampleSize));
            }

            return sizes;
        }

        var offset = stsz.DataOffset + 12;
        for (var index = 0; index < sampleCount; index++)
        {
            sizes.Add(checked((int)ReadUInt32(data, offset + (index * 4))));
        }

        return sizes;
    }

    private static List<long> ReadChunkOffsets(byte[] data, Atom stbl)
    {
        var stco = FindChild(data, stbl.DataOffset, stbl.EndOffset, "stco");
        if (stco is not null)
        {
            var count = checked((int)ReadUInt32(data, stco.Value.DataOffset + 4));
            var offsets = new List<long>(count);
            var offset = stco.Value.DataOffset + 8;
            for (var index = 0; index < count; index++)
            {
                offsets.Add(ReadUInt32(data, offset + (index * 4)));
            }

            return offsets;
        }

        var co64 = RequireChild(data, stbl, "co64");
        var co64Count = checked((int)ReadUInt32(data, co64.DataOffset + 4));
        var co64Offsets = new List<long>(co64Count);
        var co64Offset = co64.DataOffset + 8;
        for (var index = 0; index < co64Count; index++)
        {
            co64Offsets.Add(checked((long)ReadUInt64(data, co64Offset + (index * 8))));
        }

        return co64Offsets;
    }

    private static List<SampleToChunk> ReadSampleToChunks(byte[] data, Atom stsc)
    {
        var count = checked((int)ReadUInt32(data, stsc.DataOffset + 4));
        var entries = new List<SampleToChunk>(count);
        var offset = stsc.DataOffset + 8;

        for (var index = 0; index < count; index++)
        {
            entries.Add(new SampleToChunk(
                checked((int)ReadUInt32(data, offset)),
                checked((int)ReadUInt32(data, offset + 4))));
            offset += 12;
        }

        return entries;
    }

    private static List<SampleRange> BuildSampleRanges(
        IReadOnlyList<int> sampleSizes,
        IReadOnlyList<long> chunkOffsets,
        IReadOnlyList<SampleToChunk> sampleToChunks)
    {
        var ranges = new List<SampleRange>(sampleSizes.Count);
        var sampleIndex = 0;
        var tableIndex = 0;

        for (var chunkIndex = 1; chunkIndex <= chunkOffsets.Count && sampleIndex < sampleSizes.Count; chunkIndex++)
        {
            if (tableIndex + 1 < sampleToChunks.Count && chunkIndex >= sampleToChunks[tableIndex + 1].FirstChunk)
            {
                tableIndex++;
            }

            var offset = chunkOffsets[chunkIndex - 1];
            for (var sampleInChunk = 0;
                 sampleInChunk < sampleToChunks[tableIndex].SamplesPerChunk && sampleIndex < sampleSizes.Count;
                 sampleInChunk++)
            {
                var size = sampleSizes[sampleIndex++];
                ranges.Add(new SampleRange(checked((int)offset), size));
                offset += size;
            }
        }

        return ranges;
    }

    private static void DecodeFrame(ReadOnlySpan<byte> sample, byte[] frame, int width, int height)
    {
        if (sample.Length < 8)
        {
            return;
        }

        var declaredSize = ReadUInt32(sample, 0) & 0x3FFFFFFF;
        if (declaredSize < 8)
        {
            return;
        }

        var position = 4;
        var header = ReadUInt16(sample, position);
        position += 2;

        var startLine = 0;
        var lineCount = height;
        if ((header & 0x0008) != 0)
        {
            startLine = ReadUInt16(sample, position);
            position += 4;
            lineCount = ReadUInt16(sample, position);
            position += 4;
        }

        for (var y = startLine; y < startLine + lineCount && y < height && position < sample.Length; y++)
        {
            var x = 0;
            var skip = sample[position++];
            if (skip == 0)
            {
                return;
            }

            x += skip - 1;

            while (position < sample.Length)
            {
                var code = unchecked((sbyte)sample[position++]);
                if (code == 0)
                {
                    if (position >= sample.Length)
                    {
                        return;
                    }

                    var extraSkip = sample[position++];
                    if (extraSkip == 0)
                    {
                        return;
                    }

                    x += extraSkip - 1;
                }
                else if (code == -1)
                {
                    break;
                }
                else if (code > 0)
                {
                    for (var count = 0; count < code && position + 4 <= sample.Length; count++)
                    {
                        WriteArgbPixel(frame, width, height, x++, y, sample[position], sample[position + 1], sample[position + 2], sample[position + 3]);
                        position += 4;
                    }
                }
                else
                {
                    if (position + 4 > sample.Length)
                    {
                        return;
                    }

                    var repeats = -code;
                    var alpha = sample[position];
                    var red = sample[position + 1];
                    var green = sample[position + 2];
                    var blue = sample[position + 3];
                    position += 4;

                    for (var count = 0; count < repeats; count++)
                    {
                        WriteArgbPixel(frame, width, height, x++, y, alpha, red, green, blue);
                    }
                }
            }
        }
    }

    private static BitmapSource CreateBitmap(byte[] argb, int width, int height)
    {
        var stride = width * 4;
        var premultipliedBgra = new byte[argb.Length];

        for (var index = 0; index < argb.Length; index += 4)
        {
            var alpha = argb[index];
            premultipliedBgra[index] = Premultiply(argb[index + 3], alpha);
            premultipliedBgra[index + 1] = Premultiply(argb[index + 2], alpha);
            premultipliedBgra[index + 2] = Premultiply(argb[index + 1], alpha);
            premultipliedBgra[index + 3] = alpha;
        }

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Pbgra32,
            null,
            premultipliedBgra,
            stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte Premultiply(byte color, byte alpha)
    {
        return (byte)((color * alpha + 127) / 255);
    }

    private static void WriteArgbPixel(byte[] frame, int width, int height, int x, int y, byte alpha, byte red, byte green, byte blue)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        var offset = ((y * width) + x) * 4;
        frame[offset] = alpha;
        frame[offset + 1] = red;
        frame[offset + 2] = green;
        frame[offset + 3] = blue;
    }

    private static Atom RequireChild(byte[] data, Atom parent, string type)
    {
        return FindChild(data, parent.DataOffset, parent.EndOffset, type)
            ?? throw new InvalidDataException($"Missing required {type} atom.");
    }

    private static Atom? FindChild(byte[] data, int start, int end, string type)
    {
        return Children(data, start, end).FirstOrDefault(atom => atom.Type == type);
    }

    private static IEnumerable<Atom> Children(byte[] data, int start, int end)
    {
        var offset = start;
        while (offset + 8 <= end)
        {
            var size = (long)ReadUInt32(data, offset);
            var headerSize = 8;
            if (size == 1)
            {
                size = checked((long)ReadUInt64(data, offset + 8));
                headerSize = 16;
            }
            else if (size == 0)
            {
                size = end - offset;
            }

            if (size < headerSize || offset + size > end)
            {
                yield break;
            }

            yield return new Atom(ReadAscii(data, offset + 4, 4), offset + headerSize, checked((int)(offset + size)));
            offset = checked((int)(offset + size));
        }
    }

    private static string ReadAscii(byte[] data, int offset, int count)
    {
        return System.Text.Encoding.ASCII.GetString(data, offset, count);
    }

    private static ushort ReadUInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    private static ulong ReadUInt64(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset, 8));
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
    }

    private readonly record struct Atom(string Type, int DataOffset, int EndOffset);
    private readonly record struct SampleToChunk(int FirstChunk, int SamplesPerChunk);
    private readonly record struct SampleRange(int Offset, int Size);
}
