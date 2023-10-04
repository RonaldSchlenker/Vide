module UseCases.For

open Vide
open type Vide.Html

let simpleFor =
    vide {
        for x in 0..5 do
            div.class'("card") { $"I'm element no. {x}" }
    }

let statelessFor =
    vide {
        let! items = ofMutable {[]}
        let nextNum() = 0 :: items.Value |> List.max |> (+) 1
        let add1 _ = items := items.Value @ [nextNum()]
        let add100 _ = items := items.Value @ [ for _ in 0..100 do nextNum() ]
        let removeAll _ = items :=  []

        button.onclick(add1) { "Add One" }
        button.onclick(add100) { "Add 100" }
        button.onclick(removeAll) { "Remove All" }
        
        for x in items.Value do
            div.class'("card") {
                let removeMe _ = items := items.Value |> List.except [x]
                button.onclick(removeMe) { $"Remove {x}" }
        }
    }

let statefulFor =
    vide {
        let! items = ofMutable {[]}
        let nextNum() = 0 :: items.Value |> List.max |> (+) 1
        let add1 _ = items := items.Value @ [nextNum()]
        let add100 _ = items := items.Value @ [ for _ in 0..100 do nextNum() ]
        let removeAll _ = items := []

        button.onclick(add1) { "Add One" }
        button.onclick(add100) { "Add 100" }
        button.onclick(removeAll) { "Remove All" }
        
        for x in items.Value do
            div.class'("card") {
                let removeMe _ = items := items.Value |> List.except [x]
                button.onclick(removeMe) { $"Remove {x}" }

                let! count = ofMutable {0}
                button.onclick(fun _ -> count -= 1) { "dec" }
                $"{count.Value}  "
                button.onclick(fun _ -> count += 1) { "inc" }
        }
    }
