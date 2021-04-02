namespace TG275Checklist.Model

open ChecklistTypes

module ChecklistFunctions =

    let createChecklistItemWithAsyncToken planDetails listItem =
        { listItem with 
            AsyncToken = 
                listItem.Function |> runEsapiFunction planDetails }

    let createCategoryChecklistWithAsyncTokens planDetails checklist = 
        { checklist with 
            Checklist = checklist.Checklist |> List.map(fun x -> x |> createChecklistItemWithAsyncToken planDetails)}

    let createFullChecklistWithAsyncTokens planDetails checklist =
        checklist |> List.map (fun x -> x |> createCategoryChecklistWithAsyncTokens planDetails)

    let createCategoryChecklist category list =
        {
            Category = category
            Checklist = list 
                        |> List.map(fun (text, fxn) -> 
                            { initChecklistItem with 
                                Text = text
                                Function = fxn 
                            })
        }

    let tempCreateCategoryChecklistSansFunction category list =
        {
            Category = category
            Checklist = list 
                        |> List.map(fun text -> 
                            { initChecklistItem with 
                                Text = text
                                Function = None 
                            })
        }
            
