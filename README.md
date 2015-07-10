This is a temporary location for this project.

# FsBeaker
* F# implementation for [BeakerNotebook](http://beakernotebook.com/).

# Installation
* Download [BeakerNotebook](http://beakernotebook.com/getting-started?scroll)
* Modify `src/main/web/plugin/init/addevalplugins.js` in beaker directory and add the below code

```
"FSharp": { url : "./plugins/eval/fsharp/fsharp.js", bgColor: "#378BBA", fgColor: "#FFFFFF", borderColor: "", shortName: "F#" }
```

* Download [F# plugin binaries](https://github.com/BayardRock/FsBeaker/releases)
* **When using Windows be sure to Unblock before unzipping contents**
* Copy `{pluginbinaries}/eval/fsharp` directory to `{beakerisntall}/config/plugins/eval/fsharp`
* Run beaker

# Examples

* [Graphs example](http://sharing.beakernotebook.com/gist/anonymous/3de61b0b2f258b2f140b)
* [Auto translation example](http://sharing.beakernotebook.com/gist/anonymous/74dfd416da6ade4ebfe5)

#Features
* FSharp.Charting support
* Beaker charts support
* Intellisense

![Intellisense for References](https://raw.githubusercontent.com/BayardRock/FsBeaker/master/docs/files/img/intellisense-reference.gif "Intellisense for References")
![Intellisense for WorldBankData](https://raw.githubusercontent.com/BayardRock/FsBeaker/master/docs/files/img/intellisense-worldbank.gif "Intellisense for WorldBankData")
