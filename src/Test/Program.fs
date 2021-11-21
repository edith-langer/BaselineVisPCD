open Test

open Aardium
open Aardvark.Service
open Aardvark.UI
open Suave
open Suave.WebPart
open Aardvark.Rendering.Vulkan
open Aardvark.Base
open FSharp.Data.Adaptive
open System




[<EntryPoint>]
let main args =
    let port, file = 
        match args with
        | [|port;file|] -> 
            Int32.Parse port, file
        | _ -> 
            Log.line "no file given, using default"
            4321, Path.combine [__SOURCE_DIRECTORY__; "positions.csv"]

    let lines = 
        File.readAllLines file 
        |> Array.map (fun l -> 
            match l.Split(";") with
            | [|position;label|] ->
                try
                    let pos = V3d.Parse(position)
                    (pos, label)
                with e -> 
                    failwithf "could not parse v3d in line:  %s in string %s" l position
            | _ -> 
                failwithf "could not parse line: %s, should be [x,y,z];label" l
        )

    printfn "%A" lines

    Aardvark.Init()
    Aardium.init()


    let app = new Aardvark.Application.Slim.VulkanApplication()

    WebPart.startServer port [
        MutableApp.toWebPart' app.Runtime false (App.start (App.app file))
    ] |> ignore
    
    Aardium.run {
        title "Aardvark rocks \\o/"
        width 1024
        height 768
        url (sprintf "http://localhost:%d/" port)
    }

    0
