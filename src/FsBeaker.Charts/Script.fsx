#r "../../packages/Newtonsoft.Json.6.0.6/lib/net40/Newtonsoft.Json.dll"
#load "Charts.fs"

open System
open Newtonsoft.Json
open FsBeaker.Charts

let data = [1..100] |> Seq.map (fun x -> x, x)

let plot = 
    [
        BkChart.Line (data, DisplayName = "Hello 1")
        BkChart.Line (data, DisplayName = "Hello 3")
        BkChart.Line (data, DisplayName = "Hello 4")
        BkChart.Line (data, DisplayName = "Hello 5")
        BkChart.Line (data, DisplayName = "Hello 2")
    ]
    |> BkChart.Plot

Plot()
    .WithHeight(640)
    .Graphs(
        [
            Line().Data([0..100])
            Area().Data([100..200])
        ]
    )