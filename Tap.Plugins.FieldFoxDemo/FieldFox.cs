//<copyright>
// Author: MyName
// Copyright:   Copyright 2018 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Keysight.Tap;

// <template info>
//Note this template assumes that you have a SCPI based instrument, and accordingly
//extends the ScpiInstrument base class.

//If you do NOT have a SCPI based instrument, you should modify this instance to extend
//the (less powerful) Instrument base class.
//</template info>

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("FieldFox", Group: "FieldFoxDemoInstruments", Description: "FieldFox N9917A")]
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

        //<summary>
        //Select the Spectrum Analyzer Instrument
        //Activate FM Wide listening mode
        //Tune to radio station
        //Turn On the Pre-Amp
        //</summary>
        public void RadioMode(double StationFrequency)
        {
            ScpiCommand(@"INSTrument:SELect ""SA"""); 
            ScpiCommand("SENSe:MEASurement:TAListen FMW"); 
            ScpiCommand(":SENSe:TAListen:TFReq " + StationFrequency); 
            ScpiCommand(":SENSe:POWer:RF:GAIN:STATe 1"); 
        }

        //<summary>
        //Set the center frequency to the value held in the CenterFrequecy
        //<summary>
        public void SAView(double CenterFrequency)
        {
            ScpiCommand(":SENSe:FREQuency:CENTer " + CenterFrequency); 
        }

        //<summary>
        //Set Start Frequency
        //Set StopFrequency
        //<summary>
        public void ScanStations(double StartFrequency, double StopFrequency)
        {
            ScpiCommand(":SENS:FREQ:STAR " + StartFrequency); 
            ScpiCommand(":SENS:FREQ:STOP " + StopFrequency); 
        }

        public double[] GetData()
        {
            ScpiCommand("SWE:POIN 401");
            ScpiCommand("TRAC1:TYPE AVG");
            ScpiCommand("DISPlay:WINDow:TRACe:Y:SCALe:AUTO");
            ScpiCommand("INITiate:CONTinuous 0");

            return ScpiQuery<double[]>("TRAC1:DATA?");
        }


        public List<double> RoundMeasurements(double[] MeasurementResults)
        {
            var RoundedMeasurementResultsList = MeasurementResults.ToList();
            var x = 0;
            foreach (double i in MeasurementResults)
            {
                RoundedMeasurementResultsList[x] = Math.Round(MeasurementResults[x], 2);
                x++;
            }

            return RoundedMeasurementResultsList;

        }


        //<summary>
        // This function generates a list of frequencies based on the start frequency, stop frequency and number of points that are
        // displayed on the FieldFox. These frequency values are used on the x-axis of the 'FM Spectrum View' plot and for later operations.
        //</summary>
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

        //<summary>
        // Returns a list of Amplitudes from the MeasurementResults array that have a value greater than the 'AmplitudeCutoff' variable
        //</summary>
        public double[] AmplitudesAboveCutoff(int AmplitudeCutOff, double[] MeasurementResults)
        {
            var AmplitudesAboveCutoff = MeasurementResults.Where(item => item >= AmplitudeCutOff).ToArray();
            return AmplitudesAboveCutoff; 
        }

        //<summary>
        // This function takes the array of amplitude data gathered by the fieldfox(MeasurementResults) and for any
        // amplitude value greater than the 'AmplitudeCutOff' variable it adds the 'FrequencyList' value with the same
        // index array to a new array called 'FrequenciesAboveCutoff'array is returned in the last step. We now have
        // an array of Frequency values for the AmplitudesAboveCutoff array created in the previous step.
        //</summary>
        public List<double> FrequenciesAboveCutoff(int AmplitudeCutOff, double[] MeasurementResults, List<double> FrequencyList)
        {
            List<double> FrequenciesAboveCutoff = new List<double>();
            var x = 0;
            foreach (double i in MeasurementResults)
            {
                if (MeasurementResults[x] > AmplitudeCutOff)
                {
                    FrequenciesAboveCutoff.Add(FrequencyList[x]);
                    x++;
                }
                else
                {
                    x++;
                }
            }
            return FrequenciesAboveCutoff; 
        }

  
    }   
  
  }


