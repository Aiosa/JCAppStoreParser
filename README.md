# JCAppStoreParser
This tool handles JCAppstore content parsing - data validation and management.

You can download the store here:https://github.com/JavaCardSpot-dev/JCAppStore/releases to see the results -once started, open 'Applet Store' and browse its contents.
The "Applet store" section is generated based on json file. This file and the whole store content is managed and checked by this tool.

 1) Download the store content from github: https://github.com/petrs/JCAppStoreContent
 2) Use the tool over the repository to manage the data.

### Dependencies:
 - **HtmlAgilityPack**, version used `1.11.23`
 - **ini-parser**, version used `2.5.2` 
 - **Newtonsoft.JSON**, version used `12.0.3`

### Customization:
The parser includes JCParser.options file allowing custom editor. It also includes a gpg executable path in case your gpg is not in $PATH or of a different name (gpg2).

    [JCParser]
    editor = [executable]               # editor to call using windows shell cmd.exe, the editor should run outside the shell - assuming GUI (no shell window is created); default notepad
    editor_file_arg = [arg here]        # argument required by your editor to specify an input file; default empty ('notepad file' works).
    gnupg_executable = [executable]     # gpg callable command, e.g: C:\Path\To\GPG\gpg.exe or just gpg if in $PATH; default gpg


Examples:
To show help
   `--help`

To edit the file in internal editor, run with arguments: 
   `--edit --file [path\to\the\repository\]JCAppStoreContent\info_en.json`
or
   `-e -f [path\to\the\repository\]JCAppStoreContent\info_en.json`
   
To generate a translation file from 'info_en.json', use
   `--to-meta --file [path\to\the\repository\]JCAppStoreContent\info_en.json --meta [my file name]`
or this to output to stdout
   `--to-meta --file [path\to\the\repository\]JCAppStoreContent\info_en.json`
   
To generate a json info from translation file based on 'info_en.json', use
   `--to-file [language tag] --file [path\to\the\repository\]JCAppStoreContent\info_en.json --meta [my file name]`
or read the translation from stdin stdin
   `--to-file [language tag] --file [path\to\the\repository\]JCAppStoreContent\info_en.json`  
It will create the a info_[language tag].json file with the exact content as given json, but translated based on the meta file.

To perform json validation, use 
   `--validate --file [path\to\the\repository\]JCAppStoreContent\info_en.json` 
The same can be done within an editor.

To generate missing signatures, use (actually, the directory name can be anything, we just assume you downloaded the store repository)
   `--gen-sign [your PGP key ID to use] --directory [path\to\the\repository\]JCAppStoreContent` 

To regenerate all signatures, use
   `--re-sign [your PGP key ID to use] --directory [path\to\the\repository\]JCAppStoreContent`
   
NOTE: Signature operations require GnuPG.