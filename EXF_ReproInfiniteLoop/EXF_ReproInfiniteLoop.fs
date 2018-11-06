// Copyright 2018 Elmish.XamarinForms contributors. See LICENSE.md for license.
namespace EXF_ReproInfiniteLoop

open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Xamarin.Forms
open System.Threading
open System.Collections.Generic

module Workaround =
    let throttle fn timeout =
        let mailbox = MailboxProcessor.Start(fun agent ->
            let rec loop lastMsg = async {
                let! r = agent.TryReceive(timeout)
                match r with
                | Some msg ->
                    return! loop (Some msg)
                | None when lastMsg.IsSome ->
                    fn lastMsg.Value
                    return! loop None
                | None ->
                    return! loop None
            }
            loop None
        )
        mailbox.Post

module WorkaroundV2 =
    let debounce<'a> =
        let memoizations = Dictionary<obj, CancellationTokenSource>(HashIdentity.Structural)

        fun (timeout: int) (fn: 'a -> unit) value ->
            let key = fn.GetType()

            // Cancel previous debouncer
            match memoizations.TryGetValue(key) with
            | true, cts -> cts.Cancel()
            | _ -> ()

            // Create a new cancellation token and memoize it
            let cts = new CancellationTokenSource()
            memoizations.[key] <- cts

            // Start a new debouncer
            (async {
                try
                    // Wait timeout to see if another event will cancel this one
                    do! Async.Sleep timeout

                    // If still not cancelled, then proceed to invoke the callback and discard the unused token
                    memoizations.Remove(key) |> ignore
                    fn value
                with
                | _ -> ()
            })
            |> (fun task -> Async.StartImmediate(task, cts.Token))

module App = 
    type Model = 
      { Text : string
        TextWorkaround: string
        TextWorkaroundV2: string }

    type Msg =
        | TextChanged of string
        | TextChangedWorkaround of string
        | TextChangedWorkaroundV2 of string

    let init () = { Text = ""; TextWorkaround = ""; TextWorkaroundV2 = "" }, Cmd.none

    let update msg model =
        match msg with
        | TextChanged text -> { model with Text = text }, Cmd.none
        | TextChangedWorkaround text -> { model with TextWorkaround = text }, Cmd.none
        | TextChangedWorkaroundV2 text -> { model with TextWorkaroundV2 = text }, Cmd.none

    let view (model: Model) (dispatch: Msg -> unit) =
        let throttledDispatch = fixf Workaround.throttle dispatch 250

        View.ContentPage(
          content = View.StackLayout(padding = 20.0,
            children = [
                View.Label(text="Entry with infinite loop issue")
                View.Entry(text=model.Text, verticalOptions = LayoutOptions.CenterAndExpand, textChanged=(fun e ->
                    System.Console.WriteLine("TextChanged: " + e.NewTextValue)
                    dispatch (TextChanged e.NewTextValue))
                )
                View.Label(text="Entry with workaround")
                View.Entry(text=model.TextWorkaround, verticalOptions = LayoutOptions.CenterAndExpand, textChanged=(fun e ->
                    System.Console.WriteLine("TextChanged Workaround: " + e.NewTextValue)
                    throttledDispatch (TextChangedWorkaround e.NewTextValue))
                )
                View.Label(text="Entry with workaround v2")
                View.Entry(
                    text=model.TextWorkaroundV2,
                    verticalOptions=LayoutOptions.CenterAndExpand,
                    textChanged=(WorkaroundV2.debounce 250 (fun e -> dispatch (TextChangedWorkaroundV2 e.NewTextValue)))
                )
            ]))

type App () as app = 
    inherit Application ()

    let runner = 
        Program.mkProgram App.init App.update App.view
        |> Program.withConsoleTrace
        |> Program.runWithDynamicView app