﻿namespace Vide

type VideApp<'v,'s,'c>(content: Vide<'v,'s,'c>, ctxCtor: unit -> 'c, ctxFin: 'c -> unit) =
    let mutable currValue = None
    let mutable currentState = None
    let mutable isEvaluating = false
    let mutable hasPendingEvaluationRequests = false
    let mutable evaluationCount = 0uL
    let mutable suspendEvaluation = false

    interface IApp with
        member this.RequestEvaluation() =
            if suspendEvaluation then
                hasPendingEvaluationRequests <- true
            else
                // During an evaluation, requests for another evaluation can
                // occur, which have to be handled as _subsequent_ evaluations!
                let rec eval () =
                    do hasPendingEvaluationRequests <- false
                    do isEvaluating <- true
                    let value,newState = 
                        let ctx = ctxCtor ()
                        let gc = 
                            { 
                                evaluationManager = this.EvaluationManager
                                context = ctx
                            }
                        let (Vide videContent) = content
                        let res = videContent currentState gc
                        do ctxFin ctx
                        res
                    do
                        currValue <- Some value
                        currentState <- Some newState
                        isEvaluating <- false
                        evaluationCount <- evaluationCount + 1uL
                    if hasPendingEvaluationRequests then
                        eval ()
                do
                    match isEvaluating with
                    | true -> hasPendingEvaluationRequests <- true
                    | false -> eval ()
        member _.Suspend() =
            do suspendEvaluation <- true
        member this.Resume() =
            do suspendEvaluation <- false
            if hasPendingEvaluationRequests then
                (this :> IApp).RequestEvaluation()
    member this.EvaluationManager = this :> IApp
    member _.CurrentState = currentState

type VideAppFactory<'c>(ctxCtor, ctxFin) =
    let start (app: VideApp<_,_,'c>) =
        do app.EvaluationManager.RequestEvaluation()
        app
    member _.Create(content) : VideApp<_,_,'c> =
        VideApp(content, ctxCtor, ctxFin)
    member this.CreateWithUntypedState(content) : VideApp<_,_,'c> =
        let content =
            Vide <| fun (s: obj option) gc ->
                let typedS = s |> Option.map (fun s -> s :?> 's)
                let v,s = content typedS gc
                let untypedS = s |> Option.map (fun s -> s :> obj)
                v,untypedS
        this.Create(content)
    member this.CreateAndStart(content) : VideApp<_,_,'c> =
        this.Create(content) |> start
    member this.CreateAndStartWithUntypedState(content) : VideApp<_,_,'c> =
        this.CreateWithUntypedState(content) |> start
