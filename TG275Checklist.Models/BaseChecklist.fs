namespace TG275Checklist.Model

open ChecklistTypes
open ChecklistFunctions
open EsapiCalls

module BaseChecklist = 

    let prescriptionChecklist =
        [
            "Prescription (with respect to standard of care, institutional clinical guidelines or clinical trial is applicable)", Some getFullPrescription
            "Final plan and prescription approval by physician", Some getPrescriptionVsPlanApprovals
            "Site and laterality (incl. medical chart to confirm laterality)", None // TODO
            "Prescription vs consult note (e.g. physician report in EMR on plans for treatment)", None
            "Total dose, dose/fractionation, number of fractions", Some getPrescriptionVsPlanDose
            "Fractionation pattern and regimen (e.g. daily, BID, Quad Shot, regular plan follow by boost, etc.)", None // TODO? with database scrpiting
            "Energy matches prescription", Some getPrescriptionVsPlanEnergy                                         // TODO matching check
            "Modality (e.g. electrons, photons, protons, etc.)", Some getPrescriptionVsPlanModality
            "Technique (e.g. 3D, IMRT, VMAT, SBRT, etc.) matches prescription", Some getPrescriptionVsPlanTechnique     // TODO lookup table check
            "Bolus", Some getPrescriptionVsPlanBolus
            "Additional shielding (e.g. eye shields, testicular shields, etc. as applicable)", None
            "Course intent/diagnosis", Some getCoursePlanIntentDiagnosis
        ] |> createCategoryChecklist Prescription

    let simulationChecklist =
        [
            "Physician directive for imaging technique, setup and immobilization (this may include: contrast, scanning orientation, immobilization device, etc.)", None
            "Description of target location on physician planning directive (e.g. RUL Lung, H&N, L1‐L4)", None
            "Patient set up, positioning and immobilization*: (a) Appropriate for site and/or per clinical standard procedures, (b) Written or photographic documentation of patient positioning, immobilization and ancillary devices, including setup note", None
            "Image quality and usability: CT Scan Artifacts, Scan sup/inf Range Includes Enough Data, Scan FOV encompasses all required information, Use of Contrast", None
            "Motion management: (a) MD directive, (b) breath‐hold parameters, (c) gating parameters, (d) 4D‐CT parameters and data set", None
            "Registration/Fusion of image sets (CT, PET, MRI, etc.)", None                                  // TODO
            "Patient Orientation ‐ CT information matches patient setup", Some getPatientOrientations
            "Transfer and selection of image set in treatment planning system", None                        // TODO
        ] |> createCategoryChecklist Simulation

    let contouringChecklist =
        [
            "Target(s)* ‐ e.g. discernible errors, missing slices, mislabeling, gross anatomical deviations.", Some getTargetInfo
            "Organs‐at‐risk (OAR's)", Some getOARInfo
            "PTV and OAR Margin ‐ as specified in the chart and/or per protocol", None      // TODO
            "Body/External contour", Some getBodyInfo
            "Density overrides applied as needed (ex. High‐Z material, contrast, artifacts, etc.)", Some getHUOverrides
            "Consideration of Supporting Structures (i.e. couch, immobilization and ancillary devices, etc.)", Some getCouchStructures
            "Approval of contours by MD", Some getContourApprovals
        ] |> createCategoryChecklist Contouring

    let standardOperatingProceduresChecklist =
        [
            "Course and plan ID", Some getCourseAndPlanId
            "Treatment technique (e.g. 3D, IMRT, VMAT, SBRT, etc.)", Some getPlanTechnique
            "Delivery system (e.g. standard linac, CyberKnife, Tomotherapy, etc. as applicable)", Some getDeliverySystem
            "Beam arrangement", Some getBeamArrangement
            "Beam deliverability", None
            "MU", Some getBeamMUs
            "Energy", Some getBeamEnergies
            "Dose rate", Some getBeamDoseRates
            "Field delivery times", None        // TODO
            "Field size and aperture", None
            "Bolus utilization", Some getBeamBolus
            "Beam modifiers (e.g. wedges, electron and photon blocks, trays, etc.)", None
            "Treatment plan warnings/errors", Some getCalculationLogs
            "Tolerance table", Some getBeamToleranceTables
            "Potential for collision", None
            "Setup shifts use standard SOP", None
            "Physics consult (e.g. evaluation of dose to pacemaker, previous treatment, etc.)", None
        ] |> createCategoryChecklist StandardProcedure

    let planQualityChecklist =      // TODO
        [
            "Target coverage and target planning objectives", Some getTargetCoverage
            "Sparing of OARs and OAR planning objectives", Some getOARMaxDoses
            "Plan conforms to clinical trial (as applicable)", None
            "Structures used during optimization", None
            "Physician designed apertures", None
            "Dose distribution", Some getTargetCIs
            "Hot spots", Some getHotspotLocation
            "Reference points", Some getReferencePoints
            "Plan normalization", Some getPlanNormalization
            "Calculation algorithm and calculation grid size", Some getCalculationAlgorithmInfo
            "Prior Radiation accounted for in plan", None
            "Plan Sum (e.g. Original plus boost plans)", None
        ] |> createCategoryChecklist PlanQuality
       
    let doseVerificationChecklist =
        [
            "Second calculation check and/or QA performed", None
            "Verification plan for patient specific QA measurement", None   // TODO
            "Request for in‐vivo dosimetry", None
        ] |> createCategoryChecklist Verification
        
    let isocenterChecklist =
        [
            "Isocenter: placement and consistency between patient marking and setup instructions", Some getIsocenterPlacement
            "Additional shifts", Some getShifts
            "Multiple isocenters", Some getNumberOfIsocenters
        ] |> createCategoryChecklist Isocenter
        
    let imageGuidanceChecklist =
        [
            "Matching instructions (e.g. 2D/2D, 3D, etc.) and MD directive for IGRT", None
            "Matching structures", None
            "Reference CT", None
            "Isocenter on reference image(s), 2D or 3D", None
            "DRR association", None
            "DRR image quality", None
            "Imaging technique", None
            "Imaging regimen (e.g. daily, weekly, daily followed by weekly, etc.)", None
            "Parameters and setup for specialized devices (e.g. ExacTrac, VisionRT, RPM, etc.)", None
            "Isocenter for specialized devices (e.g. VisionRT, ExacTrac, etc.)", None
        ] |> createCategoryChecklist ImageGuidanceSetup
        
    let schedulingChecklist =
        [
            "Plan is scheduled for treatment", Some getPlanScheduling
            "Scheduling of safety‐critical tasks (e.g. weekly chart checks, IMRT QA, etc.)", None
        ] |> createCategoryChecklist Scheduling
        
    let replanChecklist =
        [
            "Full plan check if new plan generated", Some testFunction
            "Old/new CT registration", None
            "Isocenter placement", None
            "Deformed or new contours", Some badTestFunction
            "DVH Comparison", None
            "CTV/PTV coverage", None
            "Organs at risk dose limits", None
        ] |> createCategoryChecklist Replan
        
    let deviationChecklist =
        [
            "Any unexpected deviations entered into incident learning system", Some testFunction
        ] |> createCategoryChecklist Deviations

    let fullChecklist = 
        [
            prescriptionChecklist
            simulationChecklist
            contouringChecklist
            standardOperatingProceduresChecklist
            planQualityChecklist
            doseVerificationChecklist
            isocenterChecklist
            imageGuidanceChecklist
            schedulingChecklist
            replanChecklist
            deviationChecklist
        ]