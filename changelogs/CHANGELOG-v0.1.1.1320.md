# New Beta Release

Readarr v0.1.1.1320 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- Automated API Documentation Updates recently implemented
- [Wiki Contributions](https://wiki.servarr.com/readarr) and updates welcome and encouraged on the Wiki  itself or via GitHub

# Additional Commentary

- [Lidarr v1 released](https://www.reddit.com/r/Lidarr/comments/ul0b2w/new_release_develop_v1012578/)
- [Lidarr](https://lidarr.audio/donate), [Prowlarr](https://prowlarr.com/donate), [Radarr](https://radarr.video/donate), [Readarr](https://readarr.com/donate) now accept direct bitcoin donations
- Radarr Postgres Database Support in `nightly` and `develop`
- [Lidarr Postgres Database Support in development (Draft PR#2625)](https://github.com/Lidarr/Lidarr/pull/2625)

# Releases

## Native

- [GitHub Releases](https://github.com/Readarr/Readarr/releases)

- [Wiki Installation Instructions](https://wiki.servarr.com/readarr/installation)

## Docker

- [hotio/Readarr:testing](https://hotio.dev/containers/readarr)

- [lscr.io/linuxserver/Readarr:develop](https://docs.linuxserver.io/images/docker-readarr)

## NAS Packages

- Synology - Please ask the SynoCommunity to update the base package; however, you can update in-app normally

- QNAP - Please ask the QNAP to update the base package; however, you should be able to update in-app normally

------------

# Release Notes

## v0.1.1.1320 (changes since v0.1.0.1248)

 - Fixed: Correct User-Agent api logging

 - Fixed: UI hiding search results with duplicate GUIDs

 - New: Add date picker for custom filter dates

 - Fixed: Interactive Search Filter not filtering multiple qualities in the same filter row

 - Fixed: Clarify Qbit Content Path Error

 - Fixed: Properly handle 119 error code from Synology Download Station

 - Fixed: API error when sending payload without optional parameters

 - Fixed: Cleanup Temp files after backup creation

 - Fixed: Loading old commands from database

 - New: Update Cert Validation Help Text

 - Fixed: Clarify Indexer Priority Helptext

 - Fixed: Improve help text for download client Category

 - Fixed: IPv4 instead of IP4

 - New: Add Validations for Recycle Bin Folder

 - New: .NET 6.0.3

 - Fixed: Healthcheck warning message used incorrect variable

 - Fixed: Assume SABnzbd develop version is 3.0.0 if not specified

 - Fixed: Updater version number logging

 - Fixed: Update from version in logs

 - New: Add more information about Windows service to installer

 - Fixed: Recycle bin log message

 - Fixed: Make authentication cookie name unique to Readarr

 - Other bug fixes and improvements, see GitHub history
