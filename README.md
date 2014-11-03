This is a temporary location for this project.

# FsBeaker
* F# implementation for [BeakerNotebook](http://beakernotebook.com/).

# Installation
* Download [BeakerNotebook](http://beakernotebook.com/getting-started?scroll)
* Modify `src/main/web/plugin/init` in beaker directory and add the below code

```
"FSharp": { url : "./plugins/eval/fsharp/fsharp.js", bgColor: "#378BBA", fgColor: "#FFFFFF", borderColor: "", shortName: "F#" }
```

* Download [F# plugin binaries](https://github.com/BayardRock/FsBeaker/releases)
* Copy `{pluginbinaries}/eval/fsharp` directory to `{beakerisntall}/config/plugins/eval/fsharp`
* Run beaker

# Examples

* [Graphs example](http://sharing.beakernotebook.com/gist/anonymous/3de61b0b2f258b2f140b)
* [Auto translation example](http://sharing.beakernotebook.com/gist/anonymous/74dfd416da6ade4ebfe5)