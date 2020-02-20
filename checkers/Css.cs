
using System;
using System.Linq;
using HtmlAgilityPack;
using System.Collections.Generic;
using AutoCheck.Core;

namespace AutoCheck.Checkers{     
    /// <summary>
    /// Allows data validations over a WEB set of files.
    /// </summary>  
    public class Css : Checker{  
        /// <summary>
        /// The main connector, can be used to perform direct operations over the data source.
        /// </summary>
        /// <value></value>    
        public Connectors.Css Connector {get; private set;}
        /// <summary>
        /// Comparator operator
        /// </summary>
        public enum Operator{
            //TODO: must be reusable by other checkers
            MIN,
            MAX,
            EQUALS
        }  
        /// <summary>
        /// Creates a new checker instance.
        /// </summary>
        /// <param name="studentFolder">The folder containing the web files.</param>
        /// <param name="cssFile">CSS file name.</param>     
        public Css(string studentFolder, string cssFile){
            this.Connector = new Connectors.Css(studentFolder, cssFile);            
        }                 
        /// <summary>
        /// Given a CSS property, checks if its has been applied within the HTML document.
        /// </summary>
        /// <param name="htmlDoc">The HTML document that must be using the property.</param>
        /// <param name="property">The CSS property name.</param>
        /// <param name="value">The CSS property value.</param>
        /// <returns>The list of errors found (the list will be empty it there's no errors).</returns>
        public List<string> CheckIfCssPropertyHasBeenApplied(HtmlDocument htmlDoc, string property, string value = null){  
            List<string> errors = new List<string>();

            try{
                if(!Output.Instance.Disabled){
                    if(string.IsNullOrEmpty(value)) Output.Instance.Write(string.Format("Checking the '{0}' CSS property... ", property));
                    else Output.Instance.Write(string.Format("Checking the '{0}:{1}' CSS property... ", property, value));
                }
                
                this.Connector.CheckIfCssPropertyHasBeenApplied(htmlDoc, property, value);  //exception if not applied             
            }
            catch(Exception e){
                errors.Add(e.Message);
            }            

            return errors;
        }  
        /// <summary>
        ///  Given a set of CSS properties, checks how many of them has been applied within the HTML document, and if the total amount is lower, higher or equals than the expected.
        /// </summary>
        /// <param name="htmlDoc">The HTML document that must be using the property.</param>
        /// <param name="properties">A set of CSS property names.</param>
        /// <param name="expected">Expected applied amount.</param>
        /// <param name="op">Comparison operator to be used.</param>
        /// <returns>The list of errors found (the list will be empty it there's no errors).</returns>
        public List<string> CheckIfCssPropertiesAppliedMatchesAmount(HtmlDocument htmlDoc, string[] properties, int expected, Operator op = Operator.EQUALS){  
             List<string> errors = new List<string>();
             
             try{
                if(!Output.Instance.Disabled) Output.Instance.Write(string.Format("Checking the '({0})' CSS properties... ", string.Join(" | ", properties)));
                
                Output.Instance.Disable();
                int applied = 0;
                foreach(string prop in properties){
                    //this.Connector.CheckIfCssPropertyApplied can be also called, but might be better to use CheckIfCssPropertyApplied in order to unify behaviours
                    if(CheckIfCssPropertyHasBeenApplied(htmlDoc, prop).Count == 0) applied++;                   
                }

                Output.Instance.UndoStatus();
                errors.AddRange(CompareItems("Applied CSS properties missmatch:", expected, applied, op));                
            }
            catch(Exception e){
                errors.Add(e.Message);
            }            

            return errors;
        } 
        private List<string> CompareItems(string caption, int expected, int current, Operator op){
            //TODO: must be reusable by other checkers
            List<string> errors = new List<string>();
            string info = string.Format("expected->'{0}' found->'{1}'.", expected, current);

            switch(op){
                case Operator.EQUALS:
                    if(current != expected) errors.Add(string.Format("{0} {1}.", caption, info));
                    break;

                case Operator.MAX:
                    if(current > expected) errors.Add(string.Format("{0} maximum {1}.", caption, info));
                    break;

                case Operator.MIN:
                    if(current < expected) errors.Add(string.Format("{0} minimum {1}.", caption, info));
                    break;
            }

            return errors;
        }
    }    
}