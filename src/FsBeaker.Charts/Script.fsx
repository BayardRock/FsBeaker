#r "../../packages/Newtonsoft.Json.6.0.6/lib/net40/Newtonsoft.Json.dll"
#load "Charts.fs"

open System
open Newtonsoft.Json
open FsBeaker.Charts

let data =
    [1..100]
    |> Seq.map float
    |> Seq.map (fun x -> x, x)

let p = Plot()
p <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 1"))
p <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 2"))
p <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 3"))
p <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 4"))
p <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 5"))
p