namespace Test

open System
open Aardvark.Base
open Aardvark.SceneGraph
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.Rendering
open FSharp.Data.Adaptive
open Test.Model
open Aardvark.Rendering.Text

type Message =
    | ToggleModel
    | CameraMessage of FreeFlyController.Message
    | OrbitMessage of OrbitMessage
    | SetCenter of V3d

module View = 
    open System
    open System.IO
    open Aardvark.Base
    open Aardvark.Rendering
    open Aardvark.Application
    open Aardvark.Application.Slim
    open FSharp.Data.Adaptive

    open Aardvark.Rendering.Text

    let run file = 

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

        let coordinateCross = 
            Sg.coordinateCross' 5.0

        let texts = 
            let contents = 
                lines |> Array.map (fun (pos,label) -> 
                    // position label a bit above actual measurement
                    AVal.constant (Trafo3d.Scale(0.1) * Trafo3d.Translation (pos + V3d(0.0,0.0,0.2))), AVal.constant label
                )


    

            Sg.textsWithConfig { TextConfig.Default with renderStyle = RenderStyle.Billboard; color = C4b.Black } (ASet.ofArray contents)

        let coordinateCrosses = 
            lines |> Array.map (fun (pos,label) -> 
                Sg.coordinateCross' 0.5
                |> Sg.translation' pos
            ) |> Sg.ofSeq


        let afterPass = RenderPass.before "floor" RenderPassOrder.Arbitrary RenderPass.main

        let groundPlane : ISg<_> = 
            Sg.box' (C4b.DarkYellow) (Box3d.FromCenterAndSize(V3d.OOO,V3d(10.0,10.0,0.001)))
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }
            |> Sg.blendMode' BlendMode.Blend
            |> Sg.pass afterPass
            |> Sg.requirePicking
            |> Sg.noEvents
            |> Sg.withEvents [
                    Sg.onDoubleClick (fun p -> 
                        Message.SetCenter p
                    )
            ]

        let scene = 
            Sg.ofSeq [coordinateCross; coordinateCrosses]
            |> Sg.shader {
                do! DefaultSurfaces.trafo
            }

        let content = 
            Sg.ofSeq [scene |> Sg.noEvents; texts |> Sg.noEvents; groundPlane]

        content |> Sg.noEvents


module App =
    
    let initial file = { currentModel = Box; cameraState = FreeFlyController.initial; file = file; orbitState = OrbitState.create V3d.OOO 30.0 30.0 1.0  }

    let update (m : Model) (msg : Message) =
        match msg with
            | ToggleModel -> 
                match m.currentModel with
                    | Box -> { m with currentModel = Sphere }
                    | Sphere -> { m with currentModel = Box }

            | CameraMessage msg ->
                { m with cameraState = FreeFlyController.update m.cameraState msg }
            | OrbitMessage msg -> 
                { m with orbitState = OrbitController.update m.orbitState msg }
            | SetCenter p -> 
                { m with orbitState = OrbitController.update m.orbitState (OrbitMessage.SetTargetCenter p) }

    let view (m : AdaptiveModel) =

        let frustum = 
            Frustum.perspective 60.0 0.1 100.0 1.0 
                |> AVal.constant

        let text = Sg.text FontSquirrel.Hack.Regular C4b.Black (AVal.constant "bubu")  

        let sg =
            m.file |> AVal.map View.run |> Sg.dynamic

        let att =
            [
                style "position: fixed; left: 0; top: 0; width: 100%; height: 100%;background-color:gray"
            ]

        body [] [
            Aardvark.UI.Primitives.OrbitController.controlledControl m.orbitState OrbitMessage frustum (AttributeMap.ofList att) sg
            //FreeFlyController.controlledControl m.cameraState CameraMessage frustum (AttributeMap.ofList att) sg


        ]

    let app file =

        {
            initial = initial file
            update = update
            view = view
            threads = fun m -> m.cameraState |> FreeFlyController.threads |> ThreadPool.map CameraMessage
            unpersist = Unpersist.instance
        }