namespace TG275Checklist.Model

open ChecklistTypes

module ChecklistFunctions =

    let createChecklistItemWithAsyncToken planDetails listItem =
        { listItem with 
            AsyncToken = 
                listItem.Function |> runEsapiFunction planDetails }

    let createCategoryChecklistWithAsyncTokens planDetails checklist = 
        { checklist with 
            ChecklistItems = checklist.ChecklistItems |> List.map(fun x -> x |> createChecklistItemWithAsyncToken planDetails)}

    let createFullChecklistWithAsyncTokens planDetails checklist =
        checklist |> List.map (fun x -> x |> createCategoryChecklistWithAsyncTokens planDetails)

    let createCategoryChecklist category list =
        {
            Category = category
            ChecklistItems = 
                list 
                |> List.map(fun (text, fxn) -> 
                    { ChecklistItem.init with 
                        Text = text
                        Function = fxn 
                    })
            Loaded = false
            Loading = false
        }
            
