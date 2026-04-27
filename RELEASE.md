# Making A GitHub Release

Use this when you want normal users to see a simple installer download.

## Steps

1. Commit and push your changes to GitHub.
2. Create and push a version tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

3. GitHub Actions will build `WhipCursor-Installer.zip`.
4. Open your repo on GitHub.
5. Go to **Releases**.
6. Edit the new release if you want nicer notes.

Users can also download the GitHub ZIP, extract it, and run `Run Whip Cursor.cmd`.

The local release helper scripts live in `tools/`.
