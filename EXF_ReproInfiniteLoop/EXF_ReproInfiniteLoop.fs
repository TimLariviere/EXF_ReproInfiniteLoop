// Copyright 2018 Elmish.XamarinForms contributors. See LICENSE.md for license.
namespace EXF_ReproInfiniteLoop

open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Xamarin.Forms

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

module App = 
    type Model = 
      { 
        Text : string
        TextWorkaround: string
      }

    type Msg = | TextChanged of string | TextChangedWorkaround of string

    let init () = { Text = ""; TextWorkaround = "" }, Cmd.none

    let update msg model =
        match msg with
        | TextChanged text -> { model with Text = text }, Cmd.none
        | TextChangedWorkaround text -> { model with TextWorkaround = text }, Cmd.none

    let view (model: Model) dispatch =
        let throttledDispatch = fixf Workaround.throttle dispatch 250

        View.ContentPage(
          content = View.StackLayout(padding = 20.0,
            children = [
                View.Entry(text=model.Text, verticalOptions = LayoutOptions.CenterAndExpand, textChanged=(fun e ->
                    System.Console.WriteLine("TextChanged: " + e.NewTextValue)
                    dispatch (TextChanged e.NewTextValue))
                )
                View.Entry(text=model.TextWorkaround, verticalOptions = LayoutOptions.CenterAndExpand, textChanged=(fun e ->
                    System.Console.WriteLine("TextChanged Workaround: " + e.NewTextValue)
                    throttledDispatch (TextChangedWorkaround e.NewTextValue))
                )
            ]))

type App () as app = 
    inherit Application ()

    let runner = 
        Program.mkProgram App.init App.update App.view
        |> Program.withConsoleTrace
        |> Program.runWithDynamicView app