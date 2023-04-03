module App

open Fable.Core.JsInterop
open Browser
open Vide
open type Html

importSideEffects("./App.scss")

let host = document.getElementById("app")
let app = VideApp.Fable.createWithUntypedState host Components.Demo.view (fun _ _ _ -> ())
do app.EvaluationManager.RequestEvaluation()
