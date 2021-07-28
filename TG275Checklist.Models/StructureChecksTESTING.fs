namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open System.Windows.Media.Media3D
open System.Windows
open TG275Checklist.Model
open System
open CommonHelpers

module StructureChecks =

    let hellaLowResPoints (structure: Structure) =
        structure.MeshGeometry.Positions
        |> Seq.indexed
        |> Seq.filter (fun (x, _) -> x % ((Seq.length structure.MeshGeometry.Positions) / 10) = 0)
        |> Seq.map snd

    let getAdjustedStructurePoints (structure: Structure) =
        let len = Seq.length structure.MeshGeometry.Positions
        let skip = int (ceil(float len / 100.0 * 0.5))
        structure.MeshGeometry.Positions
        |> Seq.indexed
        |> Seq.filter (fun (x, _) -> x % skip = 0)
        |> Seq.map snd

    let getJaggedArrayPoints (structure: Structure) =
        ()//for z 0 to structure.

    let distanceSquared (p1: Point3D) (p2: Point3D) = pown (p1.X - p2.X) 2 + pown (p1.Y - p2.Y) 2 + pown (p1.Z - p2.Z) 2

    let getMinDistanceSquared (point: Point3D) (structure: Structure) =
        //structure.MeshGeometry.Positions
        structure
        |> hellaLowResPoints
        |> Seq.map (fun p -> distanceSquared point p)
        |> Seq.min

    let getAverageDistanceSquaredAndVariance (targetStructure: Structure) (checkStructure: Structure) =
        let targPoints = targetStructure |> hellaLowResPoints
        let len = float (Seq.length targPoints)
        let minDists = targPoints |> Seq.map (fun x -> sqrt (getMinDistanceSquared x checkStructure))
        let avgDist = (minDists |> Seq.sum) / len
        let var = (minDists |> Seq.sumBy (fun x -> pown (x - avgDist) 2)) / len
        avgDist, var

    let getGetDistsAndVars (structure: Structure) (structureSet: StructureSet) =
        let badDicomTypes = [ "SUPPORT"; "MARKER"; "BODY"; "EXTERNAL" ]
        let list = 
            structureSet.Structures 
            |> Seq.filter(fun x -> 
                (not (Seq.contains x.DicomType badDicomTypes))
                && structure.IsPointInsideSegment(x.CenterPoint)
                && x.MeshGeometry <> null 
                && x.Volume < structure.Volume)
            |> Seq.map (fun x -> x.Id, getAverageDistanceSquaredAndVariance structure x) 
            |> Seq.sortBy(fun (_, (_, var)) -> var)
            |> Seq.map (fun (id, (d, v)) -> $"    {id}\n       Distance: %0.1f{d} mm\n       STD: {sqrt v} mm")

        list |> Seq.tryHead

    let testMargins (plan: PlanSetup) =
        let badDicomTypes = [ "SUPPORT"; "MARKER"; "BODY"; "EXTERNAL" ]

        plan.StructureSet.Structures
        |> Seq.filter (fun x -> (not (Seq.contains x.DicomType badDicomTypes)))
        |> Seq.map(fun x -> 
            let start = System.DateTime.Now
            match getGetDistsAndVars x plan.StructureSet with
            | Some thing -> 
                let finish = System.DateTime.Now
                $"""{x.Id} - {Seq.length (x |> hellaLowResPoints)} points - originally {Seq.length x.MeshGeometry.Positions}{'\n'}{thing}{'\n'}    Finished in %0.1f{(finish - start).TotalSeconds} seconds"""
            | None -> $"")
        |> String.concat "\n"
        |> EsapiResults.fromString
    