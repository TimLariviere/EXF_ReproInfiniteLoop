// Copyright 2018 Elmish.XamarinForms contributors. See LICENSE.md for license.
namespace EXF_ReproInfiniteLoop

open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Xamarin.Forms

module App = 
    type Model = 
      { 
        Text : string
      }

    type Msg = | TextChanged of string

    let init () = { Text = "" }, Cmd.none

    let update msg model =
        match msg with
        | TextChanged text -> { model with Text = text }, Cmd.none

    let view (model: Model) dispatch =
        View.ContentPage(
          content = View.StackLayout(padding = 20.0, verticalOptions = LayoutOptions.Center,
            children = [
                View.Entry(text=model.Text, textChanged=(fun e ->
                    System.Console.WriteLine("TextChanged: " + e.NewTextValue)
                    dispatch (TextChanged e.NewTextValue))
                )
            ]))

type App () as app = 
    inherit Application ()

    let runner = 
        Program.mkProgram App.init App.update App.view
        |> Program.withConsoleTrace
        |> Program.runWithDynamicView app