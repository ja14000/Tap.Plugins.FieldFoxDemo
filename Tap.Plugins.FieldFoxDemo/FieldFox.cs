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
using Keysight.Tap;

//Note this template assumes that you have a SCPI based instrument, and accordingly
//extends the ScpiInstrument base class.

//If you do NOT have a SCPI based instrument, you should modify this instance to extend
//the (less powerful) Instrument base class.

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("FieldFox", Group: "FieldFoxDemo", Description: "FieldFox N9917A")]
    [ShortName("FieldFox")]
    public class FieldFox : ScpiInstrument
    {
        #region Settings
        // ToDo: Add property here for each parameter the end user should be able to change
        #endregion
        public FieldFox()
        {
            
          //ScpiCommand("SYSTem:PRESet");

        }

        public override void Open()
        {

            base.Open();

            if (!IdnString.Contains("N9"))
            {
                Log.Error("This instrument driver does not support the connected instrument.");
                throw new ArgumentException("Wrong instrument type.");
            }

        }

        public override void Close()
        {
            // TODO:  Shut down the connection to the instrument here.
            base.Close();
        }

        public void RadioMode(double StationFrequency)
        {
            ScpiCommand(@"INSTrument:SELect ""SA"""); //Select the Spectrum Analyser instrument
            ScpiCommand("SENSe:MEASurement:TAListen FMW"); //Activate FM Wide listening mode
            ScpiCommand(":SENSe:TAListen:TFReq " + StationFrequency); //Tune to radio station
            ScpiCommand(":SENSe:POWer:RF:GAIN:STATe 1"); //Turn On the Pre-Amp
    
        }

        public void SAView(double CenterFrequency)
        {
            ScpiCommand(":SENSe:FREQuency:CENTer " + CenterFrequency); //Set the center frequency to the center frequecy
            
            
        }

        public void ScanStations(double StartFrequency, double StopFrequency)
        {
            ScpiCommand(":SENS:FREQ:STAR "+StartFrequency); //Set Start Frequency
            ScpiCommand(":SENS:FREQ:STOP "+StopFrequency); //Set StopFrequency
        }

        public double[] GetData()
        { 
            ScpiCommand("SWE:POIN 401"); //Set No. of points
            ScpiCommand("TRAC1:TYPE AVG"); //Set trace type
            ScpiCommand("DISPlay:WINDow:TRACe:Y:SCALe:AUTO");
            return ScpiQuery<double[]>("TRAC1:DATA?");
        }

        public List<double> CalcFrequency(double StartFrequency, double StopFrequency)
        {
            var FrequencyStep = ((StopFrequency - StartFrequency) / (401));
            List<double> FrequencyList = new List<double>();
            FrequencyList.Add(StartFrequency);
                   
            for (int runs = 0; runs < 400; runs++)
            {
                FrequencyList.Add(FrequencyList[runs] + FrequencyStep);
            }

            return FrequencyList;
        }

        public double[] StationsFound(int AmplitudeCutOff, double[] MeasurementResults) 
        {
            var StationsFoundArray = MeasurementResults.Where(item => item >= AmplitudeCutOff).ToArray();
       
            return StationsFoundArray;
        }

        public List<double> FrequenciesFound(int AmplitudeCutOff, double[] MeasurementResults, List<double> FrequencyList)
        {
            List<double> StationFrequencyList = new List<double>();
            var x = 0;

            foreach (double i in MeasurementResults)

            {

                if (MeasurementResults[x] > AmplitudeCutOff)
                {
                    
                    StationFrequencyList.Add(FrequencyList[x]);
                    x++;
                }

                else
                {
                    x++;
                }
            }

            return StationFrequencyList;


        }
    }

    
}

