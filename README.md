# WebTV/MSN TV String Editor
A tool capable of editing the string files used by WebTV/MSN TV clients (.dir and .dat files in ROMFS under "ROM/Local"). This tool was originally written in 2021 for a WebTV Dreamcast translation project, and is now being released to the public, just in case anyone else has any intentions to modify the strings of WebTV/MSN TV based clients.

More information on WebTV/MSN TV string files: http://wiki.webtv.zone/mediawiki/index.php/ROM_String_Files

This program is written in C# .NET, and was originally created by wtv-411. This is also currently a work in progress, and things are currently unpolished or otherwise unfinished.

# Usage
Select a .dir file from an extracted WebTV ROMFS partition (Viewer or boot/app ROM), or drag and drop it into the program while it's open, and edit the contents to your heart's desire (or whatever WebTV limitations stand in your way).

# Features
- Open, view, and edit WebTV/MSN TV string files
- Word wrap and custom fonts for the editor
- Support for WebTV string files using Shift-JIS

## Planned
- Ability to change newline format in strings (Provisioned, but not currently working. Likely will be removed)
- Find functionality for text (Planned, but scrapped for the time being)

# Compatibility
Currently, this program only targets Windows and .NET Framework 4.8, so for the time being you will need a Windows installation capable of using .NET Framework 4.8 to use it. The code is platform-independent enough that porting it to something like .NET Core isn't out of the question, if possible.