﻿namespace Barb

open System
open System.Collections
open System.Collections.Generic
open System.Collections.Concurrent

open Barb.Interop
open Barb.Representation
open Barb.Parse
open Barb.Reduce

module Compiler =

    let buildExpression (localType: Type) (predicate: string) : (obj -> obj) =

        let memberMap =
            resolveAllProperties localType "" id
            |> Map.ofSeq

        let parsedTokens = parseProgram predicate

    #if DEBUG
        printfn ""
        printfn "PT: %A" parsedTokens
        printfn ""
    #endif

        let reducedExpression = 
            resolveExpression parsedTokens Map.empty false |> List.rev

    #if DEBUG
        printfn ""
        printfn "RE: %A" reducedExpression
        printfn ""
    #endif

        let calculateResult input = 
            let inputBindings = memberMap |> Map.map (fun k prop -> prop input |> resolveResultType)
    
    #if DEBUG
            printfn ""
            printfn "IB: %A" inputBindings
            printfn ""
    #endif

            resolveExpression reducedExpression inputBindings true
            |> resolveExpressionResult

        calculateResult

    let buildExpr<'I, 'O> predicate =
        let expression = buildExpression typeof<'I> predicate
        fun (input : 'I) -> 
            match expression input with
            | :? 'O as result -> result
            | untypedRes -> convertToTargetType typeof<'O> untypedRes 
                            |> function 
                               | Some (typedRes) -> typedRes :?> 'O               
                               | None -> failwith (sprintf "Unexpected output type/format: %A/%s" untypedRes (untypedRes.GetType().Name))

type BarbFunc<'I,'O> (predicate) =
    let func = Compiler.buildExpr<'I,'O> (predicate)
    member t.Execute (record: 'I) =     
        func record

