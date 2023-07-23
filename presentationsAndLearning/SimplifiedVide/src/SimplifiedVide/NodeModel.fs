[<AutoOpen>]
module Vide.NodeModel

// TODO: Make it non-generic, but use Fable directly here

open Browser
open Browser.Types

module NodeDocument =
    let ensureChildAppended (parent: Node) (child: Node) =
        if not (parent.contains child) then
            parent.appendChild(child) |> ignore
    let removeChild (parent: Node) (child: Node) =
        parent.removeChild(child) |> ignore
    let getChildren (parent: Node) =
        let nodes = parent.childNodes
        [ for i in 0 .. nodes.length-1 do nodes.Item i ]
    let clearChildren (parent: Node) =
        parent.textContent <- ""
    let createTextNode (text: string) =
        let tn = document.createTextNode(text)
        do tn.textContent <- text
        tn
    let createNodeOfName (tagName: string) =
        document.createElement(tagName)

// TODO: Inline things
type NodeContext(parent: Node) =
    let mutable keptChildren = []
    member _.CreateTextNode(value: string) =
        NodeDocument.createTextNode value
    member _.ShowChild(child) =
        // What is important here:
        // The ordering is supposed to remain unchanged!
        // So we don't need a concept of "current index"
        do keptChildren <- child :: keptChildren
        do NodeDocument.ensureChildAppended parent child
    member _.RemoveObsoleteChildren() =
        let childrenForRemoval = NodeDocument.getChildren parent |> List.except keptChildren
        for child in childrenForRemoval do
            NodeDocument.removeChild parent child
    member _.ClearContent() =
        do NodeDocument.clearChildren parent
    static member Create<'e when 'e :> Node>(thisNode: 'e) =
        NodeContext(thisNode)
    
type ChildAction = Keep | DiscardAndCreateNew

type NodeModifierContext<'e when 'e :> Node> =
    {
        node: 'e
        app: IApp
    }

type NodeModifier<'e when 'e :> Node> = NodeModifierContext<'e> -> unit

// TODO: Inline Check-functions etc.
type NodeBuilder<'e  when 'e :> Node>(tagName: string) =
    inherit VideBuilder()

    member val InitModifiers: ResizeArray<NodeModifier<'e>> = ResizeArray() with get
    member val PreEvalModifiers: ResizeArray<NodeModifier<'e>> = ResizeArray() with get
    member val PostEvalModifiers: ResizeArray<NodeModifier<'e>> = ResizeArray() with get
    
    member _.CreateContext(elem: 'e) = NodeContext.Create(elem)
    member _.CreateThisElement(ctx: NodeContext) =
        let n = NodeDocument.createNodeOfName tagName
        do ctx.ShowChild(n)
        // TODO: Can we get rid of the unsafe cast?
        (box n) :?> 'e
    member _.CheckChildNode(actualNode: Node) =
        match actualNode.nodeName.ToUpper() = tagName.ToUpper() with
        | true -> Keep
        | false -> DiscardAndCreateNew

module NodeBuilder =
    let inline run<'v1,'v2,'s,'e when 'e :> Node>
        (thisBuilder: NodeBuilder<'e>)
        (Vide childVide: Vide<'v1,'s,NodeContext>)
        (createResultVal: 'e -> 'v1 -> 'v2)
        : Vide<'v2, 'e * 's, NodeContext>
        =
        Vide <| fun s app ctx ->
            let inline runModifiers modifiers node =
                for m in modifiers do
                    m { node = node; app = app }
            let s,cs =
                match s with
                | None -> None,None
                | Some (ms,fs) -> Some ms, Some fs
            let thisElement,cs =
                // Can it happen that s is Some and cs is None? I don't think so.
                // But: See comment in definition of: Vide.Core.Vide
                match s with
                | None ->
                    let newElement,s = thisBuilder.CreateThisElement(ctx), cs
                    do runModifiers thisBuilder.InitModifiers newElement
                    newElement,s
                | Some thisElement ->
                    match thisBuilder.CheckChildNode(thisElement) with
                    | Keep ->
                        do ctx.ShowChild(thisElement)
                        thisElement,cs
                    | DiscardAndCreateNew ->
                        thisBuilder.CreateThisElement(ctx), None
            do runModifiers thisBuilder.PreEvalModifiers thisElement
            let thisCtx = thisBuilder.CreateContext(thisElement)
            let cv,cs = childVide cs app thisCtx
            do thisCtx.RemoveObsoleteChildren()
            do runModifiers thisBuilder.PostEvalModifiers thisElement
            let result = createResultVal thisElement cv
            let state = thisElement,cs
            result,state

module BuilderBricks =
    let inline yieldVide(v: Vide<_,_,_>) =
        v
    
    let inline yieldText(value: string) =
        Vide <| fun s app (ctx: NodeContext) ->
            let textNode =
                s |> Option.defaultWith (fun () ->
                    let textNode = ctx.CreateTextNode(value)
                    do ctx.ShowChild(textNode)
                    textNode
                )
            do
                if textNode.textContent <> value then
                    textNode.textContent <- value
                do ctx.ShowChild(textNode)
            (),textNode

type VideBuilder with
    member _.Return(x) = BuilderBricks.return'(x)
    member _.Delay(f) = BuilderBricks.delay(f)
    member _.Combine(a, b) = BuilderBricks.combine(a, b)
    member _.For(seq, body) = BuilderBricks.for'(seq, body)

    member _.Yield(b: VideBuilder) = b { () }
    member _.Yield(b: NodeBuilder<_>) = b { () }
    member _.Yield(v) = BuilderBricks.yieldVide(v)
    member _.Yield(value) = BuilderBricks.yieldText(value)

type NodeBuilder<'e  when 'e :> Node> with
    member _.Return(x) = BuilderBricks.return'(x)
    member _.Delay(f) = BuilderBricks.delay(f)
    member _.Combine(a, b) = BuilderBricks.combine(a, b)
    member _.For(seq, body) = BuilderBricks.for'(seq, body)
    
    member this.Run(v) = NodeBuilder.run this v (fun n v -> v)

    member _.Yield(b: VideBuilder) = b { () }
    member _.Yield(b: NodeBuilder<_>) = b { () }
    member _.Yield(v) = BuilderBricks.yieldVide(v)
    member _.Yield(value) = BuilderBricks.yieldText(value)

let vide = VideBuilder()