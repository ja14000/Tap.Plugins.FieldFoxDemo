// Author: MyName
// Copyright:   Copyright 2018 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Keysight.Tap;  // Use Platform infrastructure/core components (log,TestStep definition, etc)

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("Step", Group: "FieldFoxDemo", Description: "Tune in to a radio station, capture data, plot graphs")]
    public class Step : TestStep
    {
        #region Settings
        // ToDo: Add property here for each parameter the end user should be able to change.
        #endregion

        //Creates a UI dropdown for the variable
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StationFrequency { get; set; } 
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double CenterFrequency { get; set; }
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StartFrequency { get; set; }
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StopFrequency { get; set; }
        [Unit("dBm", UseEngineeringPrefix: true)]
        public int AmplitudeCutOff { get; set; }


        // Instrument Declarations (Creates dropdown in TAP GUI))
        public FieldFox FF { get; set; }
                
        public Step()
        {
            //Set default values for properties / settings.
            StationFrequency = 104.1e6;
            CenterFrequency = StationFrequency;
            StartFrequency = 80000000;
            StopFrequency = 108000000;
            AmplitudeCutOff = -80;

        }
                
        public override void PrePlanRun()
        {
            base.PrePlanRun();
            
        }

        public override void Run()
        {
           FF.RadioMode(StationFrequency);
           FF.SAView(CenterFrequency);
           FF.ScanStations(StartFrequency, StopFrequency);
            

           var MeasurementResults = FF.GetData();


           var StationsArray = FF.StationsFound(AmplitudeCutOff, MeasurementResults);

           var FrequencyList = FF.CalcFrequency(StartFrequency, StopFrequency);
           var FrequencyArray = FrequencyList.ToArray(); 
                                  
           var FrequenciesFoundList = FF.FrequenciesFound(AmplitudeCutOff, MeasurementResults, FrequencyList);
           var FrequenciesFoundArray = FrequenciesFoundList.ToArray();


           Results.PublishTable("FM Spectrum View", new List<string> {"Frequency", "Amplitude" },FrequencyArray, MeasurementResults);
           Results.PublishTable("Frequencies Above Cutoff", new List<string> {"Station Frequency(Hz)", "Station Amplitude(dBm)"}, FrequenciesFoundArray, StationsArray);

        }
    

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}
