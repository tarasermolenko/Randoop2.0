﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Security;
using Common;
using System.Collections.ObjectModel;

namespace Randoop
{
    public class MethodCall : Transformer
    {
        public readonly MethodInfo method;
        private readonly Type[] resultTypes;
        private readonly bool[] defaultActiveResultTypes;
        public bool DeclaringMethodOverridesEquals;

        public int timesExecuted = 0;
        public double executionTimeAccum = 0;
        public int timesReturnValRetrived = 0; //xiao.qu@us.abb.com adds

        public int ReceiverUnchangedCount = 0;

        private static Dictionary<MethodInfo, MethodCall> cachedTransformers =
            new Dictionary<MethodInfo, MethodCall>();

        public static MethodCall Get(MethodInfo field)
        {
            MethodCall t;
            cachedTransformers.TryGetValue(field, out t);
            if (t == null)
                cachedTransformers[field] = t = new MethodCall(field);
            return t;
        }

        public override string MemberName
        {
            get
            {
                return method.Name;
            }
        }


        public override int TupleIndexOfIthInputParam(int i)
        {
            if (i < 0 && i > resultTypes.Length - 2 /* retval is not an input */) throw new ArgumentException("index out of range.");
            if (i == 0) return 0; // receiver
            return (i + 1); // skip retval slot.
        }

        public override Type[] ParameterTypes
        {
            get
            {
                ParameterInfo[] parameters = method.GetParameters();
                Type[] retval = new Type[parameters.Length + 1]; // add one for receiver
                retval[0] = method.DeclaringType;
                for (int i = 0; i < parameters.Length; i++)
                    retval[i + 1] = parameters[i].ParameterType;
                return retval;
            }
        }

        public override Type[] TupleTypes
        {
            get { return resultTypes; }
        }

        public override bool[] DefaultActiveTupleTypes
        {
            get { return defaultActiveResultTypes; }
        }

        public override string ToString()
        {
            return method.DeclaringType.FullName + "." + method.ToString();
        }

        public override bool Equals(object obj)
        {
            MethodCall t = obj as MethodCall;
            if (t == null)
                return false;
            return (this.method.Equals(t.method));
        }

        public override int GetHashCode()
        {
            return method.GetHashCode();
        }

        private MethodCall(MethodInfo method)
        {
            Util.Assert(method is MethodInfo);
            this.method = method as MethodInfo;

            ParameterInfo[] pis = method.GetParameters();

            resultTypes = new Type[pis.Length + 2];
            defaultActiveResultTypes = new bool[pis.Length + 2];

            resultTypes[0] = method.DeclaringType;

            if (method.IsStatic)
                defaultActiveResultTypes[0] = false;
            else
                defaultActiveResultTypes[0] = true;

            resultTypes[1] = (method as MethodInfo).ReturnType;
            if ((method as MethodInfo).ReturnType.Equals(typeof(void)))
                defaultActiveResultTypes[1] = false;
            else
                defaultActiveResultTypes[1] = true;


            for (int i = 0; i < pis.Length; i++)
            {
                resultTypes[i + 2] = pis[i].ParameterType;
                //if (pis[i].IsOut)
                //    defaultActiveResultTypes[i + 2] = true;
                //else
                defaultActiveResultTypes[i + 2] = false;
            }

            // Exceptions
            if (method.Name.Equals("GetHashCode"))
            {
                for (int i = 0; i < defaultActiveResultTypes.Length; i++)
                    defaultActiveResultTypes[i] = false;
            }

            MethodInfo equalsMethod = method.DeclaringType.GetMethod("Equals",
                new Type[] { typeof(object) },
                null /* ok because we're using DefaultBinder */);

            if (equalsMethod != null)
                DeclaringMethodOverridesEquals = true;
            else
                DeclaringMethodOverridesEquals = false;
        }

        private string GenRegressionAssertion(object retVal, string newValueName) //xiao.qu@us.abb.com adds
        {
            StringBuilder b = new StringBuilder();
            if (retVal != null) //CASE2
            {
                b.Append("\r\n      //Regression assertion (captures the current behavior of the code)\r\n");

                if (retVal.GetType() == typeof(System.String))
                {
                    string temp = retVal.ToString().Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
                    temp = temp.Replace("\"", "\\\"");
                    b.Append("      Assert.AreEqual(" + "\"" + temp + "\", " + newValueName +
                        ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                    return b.ToString();
                }
                else //not string
                {
                    //if (retVal.GetType().IsPrimitive)
                    bool isPrimitive = false;

                    if (retVal.GetType() == typeof(bool))
                    {
                        b.Append("      Assert.AreEqual<System.Boolean>(" + retVal.ToString().ToLower() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(byte))
                    {
                        b.Append("      Assert.AreEqual<byte>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(short))
                    {
                        b.Append("      Assert.AreEqual<short>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(int))
                    {
                        b.Append("      Assert.AreEqual<int>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(long))
                    {
                        b.Append("      Assert.AreEqual<long>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(float))
                    {
                        b.Append("      Assert.AreEqual<float>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(double))
                    {
                        b.Append("      Assert.AreEqual<double>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    if (retVal.GetType() == typeof(char))
                    {
                        b.Append("      Assert.AreEqual<char>(" + retVal.ToString() + ", "
                            + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");

                        isPrimitive = true;
                    }

                    //else //CASE3
                    if (!isPrimitive)
                    {
                        //b.Append("//Return value is not primitive. \r\n");
                        b.Append("      Assert.IsNotNull(" + newValueName + ");\r\n");
                    }

                    return b.ToString();
                }

            }
            else //return value is null or execution throws exceptions
            {
                //CASE4
                //b.Append("\r\n      //Regression assertion (captures the current behavior of the code): method" + this.method.Name + "\r\n");
                //b.Append("      Assert.IsNull(" + newValueName + ",\"Regression Failure? [" + this.timesReturnValRetrived.ToString() + "]\");\r\n");
                //return value is null, no newValueName is created

                //CASE5 do nothing
                return "";
            }
        }

        // arguments[0] is the receiver. if static method, arguments[0] ignored.
        //
        // TODO: This method can be largely improved. For one, it should
        // be broken up into smaller methods depending on the nature of
        // the method (regular method, operator, property, etc.).
        public override string ToCSharpCode(ReadOnlyCollection<string> arguments, String newValueName)
        {
            StringBuilder b = new StringBuilder();
            //return value
            string retType = method.ReturnType.ToString();
            if (!method.ReturnType.Equals(typeof(void))) //if return value is null, no newValueName was given as input 
            {
                retType = SourceCodePrinting.ToCodeString(method.ReturnType);
                b.Append(retType + " " + newValueName + " = ");
            }

            //for overloaded operators
            bool isOverloadedOp = false;
            string overloadOp = "";

            bool isCastOp = false;
            string castOp = "";

            if (method.IsStatic)
            {
                //dont append the type name in case of operator overloading
                //operator overloading?

                if (ReflectionUtils.IsOverloadedOperator(method, out overloadOp))
                {
                    isOverloadedOp = true;
                }
                else if (ReflectionUtils.IsCastOperator(method, out castOp))
                {
                    isCastOp = true;
                }
                else if (method.IsSpecialName && method.Name.StartsWith("op_"))
                {
                    castOp = "(NOT HANDLED: " + method.ToString() + ")"; // TODO improve this message.
                }
                else
                {
                    isOverloadedOp = false;
                    isCastOp = false;
                    b.Append(SourceCodePrinting.ToCodeString(method.DeclaringType));
                    b.Append(".");
                }
            }
            else
            {
                Type methodDeclaringType = ParameterTypes[0];

                b.Append("((");
                b.Append(SourceCodePrinting.ToCodeString(methodDeclaringType));
                b.Append(")");
                b.Append(arguments[0]);
                b.Append(")");
                b.Append(".");

            }

            //check if its a property    
            //TODO: setter should not have a return value
            bool isSetItem = false;
            bool isItemOf = false;
            bool isGetItem = false;
            bool isChars = false;

            if (method.IsSpecialName)
            {
                string s = (method.Name);

                bool isDefaultProperty = false;
                foreach (MemberInfo mi in method.DeclaringType.GetDefaultMembers())
                {
                    if (!(mi is PropertyInfo))
                        continue;

                    PropertyInfo pi = mi as PropertyInfo;
                    if (method.Equals(pi.GetGetMethod()))
                    {
                        isDefaultProperty = true;
                        isGetItem = true;
                        b.Remove(b.Length - 1, 1); // Remove the "." that was inserted above.
                        b.Append("[");
                    }
                    else if (method.Equals(pi.GetSetMethod()))
                    {
                        isDefaultProperty = true;
                        isSetItem = true;
                        b.Remove(b.Length - 1, 1); // Remove the "." that was inserted above.
                        b.Append("[");
                    }
                }

                if (isDefaultProperty)
                {
                    // Already processed.
                }
                else if (s.StartsWith("get_ItemOf"))
                {
                    isItemOf = true;
                    b.Remove(b.Length - 1, 1); // Remove the "." that was inserted above.
                    b.Append("[");
                }
                //shuvendu: Important: replace get_Item with [] but get_ItemType with ItemType()
                // The last clause is because some classes define an "Item" property
                // that has no index.
                else if (s.Equals("get_Item") && method.GetParameters().Length > 0)
                {
                    isGetItem = true;
                    b.Remove(b.Length - 1, 1); // Remove the "." that was inserted above.
                    b.Append("[");
                }
                // The last clause is because some classes define an "Item" property
                // that has no index.
                else if ((s.Equals("set_Item") || s.Equals("set_ItemOf")) && method.GetParameters().Length > 1)
                {
                    isSetItem = true;
                    b.Remove(b.Length - 1, 1); // Remove the "." that was inserted above.
                    b.Append("[");
                }
                else if ((s.StartsWith("get_")))
                {
                    s = s.Replace("get_", "");
                    b.Append(s);
                    //                        b.Append("(");
                }
                else if (s.StartsWith("set_"))
                {
                    s = s.Replace("set_", "");
                    b.Append(s + " = ");
                }
                else if (isOverloadedOp)
                {
                    // Nothing to do here.
                }
                else if (isCastOp)
                {
                    // Nothing to do here.
                }
                else
                {
                    // Operator not handled in output generation.
                }


            }
            else
            {
                b.Append(method.Name);
                b.Append("(");
            }

            //arguments tuples 

            for (int i = 1; i < arguments.Count; i++)
            {
                //we also need to remove the typename that comes with the static method
                if (isOverloadedOp && i == 2)
                {
                    b.Append(overloadOp);
                }
                else if (isCastOp && i == 1)
                {
                    b.Append(castOp);
                }
                else if (isSetItem && i == arguments.Count - 1)
                {
                    b.Append("] = ");
                }
                else
                {
                    if (i > 1)
                        b.Append(", ");
                }


                // Cast.
                b.Append("(");
                b.Append(SourceCodePrinting.ToCodeString(ParameterTypes[i]));
                b.Append(")");
                b.Append(arguments[i]);
            }

            if (!method.IsSpecialName)
            {
                b.Append(")");
            }
            else if (isGetItem || isChars || isItemOf)
            {
                b.Append("]");
            }
            b.Append(" ;");

            #region regression assertion
            ////xiao.qu@us.abb.com adds for capture return value for regression assertion -- start////
            //CASE1 -- output-plan-only stage: the execution of the plan hasn't happen yet
            //CASE2 -- execution has happened: return value is "not null & primitive+string"
            //CASE3 -- execution has happened: return value is "not null & not primitive+string"
            //CASE4 -- execution has happened: return value is "null"
            //CASE5 -- execution has happened: execution fails, return value is "null"("RANDOOPFAIL")

            //CASE1
            //if (this.timesReturnValRetrived >= this.ReturnValue.Count) //timesExecuted == ReturnValue.Count
            if (this.timesReturnValRetrived >= this.timesExecuted)
            {
                Console.WriteLine(this.method.Name + "[" + this.timesReturnValRetrived.ToString()
                    + "+] didn't execute yet. Output-plan-only stage.");

                return b.ToString(); //skip adding regression assertion
            }

            Console.WriteLine(this.method.Name + "[" + this.timesReturnValRetrived.ToString()
                    + "] get-return-value stage.");

            object tempval = this.ReturnValue[this.timesReturnValRetrived];  // timesReturnValRetrived == ReturnValue.Count - 1

            //b.Append("\t//" + retType + "?=" + tempval.GetType().ToString() + ";\n"); //for debug

            string regressionAssert = GenRegressionAssertion(tempval, newValueName);

            b.Append(regressionAssert);

            this.timesReturnValRetrived++;

            ////xiao.qu@us.abb.com adds for capture return value for regression assertion -- end////
            #endregion regression assertion

            return b.ToString();
        }


        public override bool Execute(out ResultTuple ret, ResultTuple[] parameters,
            Plan.ParameterChooser[] parameterMap, TextWriter executionLog, TextWriter debugLog, out Exception exceptionThrown, out bool contractViolated, bool forbidNull)
        {
            this.timesExecuted++;
            long startTime = 0;
            Timer.QueryPerformanceCounter(ref startTime);

            object[] objects = new object[method.GetParameters().Length];
            // Get the actual objects from the results using parameterIndices;
            object receiver = parameters[parameterMap[0].planIndex].tuple[parameterMap[0].resultIndex];
            for (int i = 0; i < method.GetParameters().Length; i++)
            {
                Plan.ParameterChooser pair = parameterMap[i + 1];
                objects[i] = parameters[pair.planIndex].tuple[pair.resultIndex];
            }

            // FIXME This should be true! It currently isn't.
            //if (!this.coverageInfo.methodInfo.IsStatic)
            //    Util.Assert(receiver != null);

            if (forbidNull)
                foreach (object o in objects)
                    Util.Assert(o != null);

            CodeExecutor.CodeToExecute call;

            object returnValue = null;
            contractViolated = false; //default value of contract violation

            call = delegate () { returnValue = method.Invoke(receiver, objects); };


            bool retval = true;

            executionLog.WriteLine("execute method " + this.method.Name
                + "[" + (this.timesExecuted - 1).ToString() + "]"); //xiao.qu@us.abb.com changes
            Console.WriteLine("execute method " + this.method.Name //xiao.qu@us.abb.com adds
                + "[" + (this.timesExecuted - 1).ToString() + "]");

            executionLog.Flush();

            //if (this.timesExecuted != this.ReturnValue.Count + 1) //xiao.qu@us.abb.com adds for debug
            //{
            //    Console.WriteLine("timeExecute = " + this.timesExecuted.ToString() +
            //        " but ReturnValue is " + this.ReturnValue.Count.ToString());
            //}

            if (!CodeExecutor.ExecuteReflectionCall(call, debugLog, out exceptionThrown))
            {
                //for exns we can ony add the class to faulty classes when its a guideline violation
                if (Util.GuidelineViolation(exceptionThrown.GetType()))
                {

                    PlanManager.numDistinctContractViolPlans++;

                    KeyValuePair<MethodBase, Type> k = new KeyValuePair<MethodBase, Type>(this.method, exceptionThrown.GetType());
                    if (!exnViolatingMethods.ContainsKey(k))
                    {
                        PlanManager.numContractViolatingPlans++;
                        exnViolatingMethods[k] = true;
                    }

                    //add this class to the faulty classes
                    contractExnViolatingClasses[method.DeclaringType] = true;
                    contractExnViolatingMethods[method] = true;
                }

                ret = null;
                executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString()
                    + "]: invocationOk is false.");//xiao.qu@us.abb.com adds

                //string temp = "RANDOOPFAIL"; //xiao.qu@us.abb.com adds for capture current status
                this.ReturnValue.Add(null); //xiao.qu@us.abb.com adds for capture current status

                return false;
            }
            else
            {
                ret = new ResultTuple(method, receiver, returnValue, objects);

                #region caputre latest execution return value
                ////xiao.qu@us.abb.com adds to capture return value -- start////
                if (returnValue != null)
                {
                    if ((returnValue.GetType() == typeof(System.String))
                        || (returnValue.GetType() == typeof(System.Boolean))
                        || (returnValue.GetType() == typeof(byte))
                        || (returnValue.GetType() == typeof(short))
                        || (returnValue.GetType() == typeof(int))
                        || (returnValue.GetType() == typeof(long))
                        || (returnValue.GetType() == typeof(float))
                        || (returnValue.GetType() == typeof(double))
                        || (returnValue.GetType() == typeof(char)))
                        executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString() + "]: "
                        + returnValue.ToString().Replace("\n", "\\n").Replace("\r", "\\r"));
                    else
                        executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString() + "]: not string or primitive");

                    //doulbe check to make sure there is no non-deterministic exeuction -- we don't want to regression assertion with that
                    //This is not a sufficient approach because the difference may be inherited from previous constructors or method calls
                    //What was done in Randoop(java): after generating an "entire" test suite, Randoop runs it before outputting it. 
                    //If any test fails, Randoop disables each failing assertions.
                    //let the VS plug-in do this functionality
                    if (Execute2(returnValue, objects, receiver, executionLog, debugLog))
                        this.ReturnValue.Add(returnValue);
                    else
                        this.ReturnValue.Add(null);
                }
                else
                {
                    executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString()
                        + "]: no return value");

                    this.ReturnValue.Add(null);
                }
                ////xiao.qu@us.abb.com adds to capture return value -- end////
                #endregion caputre latest execution return value
            }

            //check if the objects in the output tuple violated basic contracts
            if (ret != null)
            {
                foreach (object o in ret.tuple)
                {
                    if (o == null) continue;

                    bool toStrViol, hashCodeViol, equalsViol;
                    int count;
                    if (Util.ViolatesContracts(o, out count, out toStrViol, out hashCodeViol, out equalsViol))
                    {
                        contractViolated = true;
                        contractExnViolatingMethods[method] = true;

                        PlanManager.numDistinctContractViolPlans++;

                        bool newcontractViolation = false;

                        if (toStrViol)
                        {

                            if (!toStrViolatingMethods.ContainsKey(method))
                                newcontractViolation = true;
                            toStrViolatingMethods[method] = true;
                        }
                        if (hashCodeViol)
                        {
                            if (!hashCodeViolatingMethods.ContainsKey(method))
                                newcontractViolation = true;

                            hashCodeViolatingMethods[method] = true;
                        }
                        if (equalsViol)
                        {
                            if (!equalsViolatingMethods.ContainsKey(method))
                                newcontractViolation = true;

                            equalsViolatingMethods[method] = true;
                        }

                        if (newcontractViolation)
                            PlanManager.numContractViolatingPlans++;

                        //add this class to the faulty classes
                        contractExnViolatingClasses[method.DeclaringType] = true;

                        retval = false;
                    }

                }
            }

            if (contractViolated)  //xiao.qu@us.abb.com adds
                executionLog.WriteLine("contract violation."); //xiao.qu@us.abb.com adds

            long endTime = 0;
            Timer.QueryPerformanceCounter(ref endTime);
            executionTimeAccum += ((double)(endTime - startTime)) / ((double)(Timer.PerfTimerFrequency));

            return retval;
        }


        public bool Execute2(object retValOldRun, object[] objectsOld, object receiverOld,
            TextWriter executionLog, TextWriter debugLog)
        {
            //object[] objects = new object[method.GetParameters().Length];
            //// Get the actual objects from the results using parameterIndices;
            //object receiver = parameters[parameterMap[0].planIndex].tuple[parameterMap[0].resultIndex];
            //for (int i = 0; i < method.GetParameters().Length; i++)
            //{
            //    Plan.ParameterChooser pair = parameterMap[i + 1];
            //    objects[i] = parameters[pair.planIndex].tuple[pair.resultIndex];
            //}

            //if (forbidNull)
            //    foreach (object o in objects)
            //        Util.Assert(o != null);

            object[] objects = objectsOld;
            object receiver = receiverOld;
            ResultTuple ret;
            Exception exceptionThrown = new Exception();

            CodeExecutor.CodeToExecute call;

            object returnValue = null;

            call = delegate () { returnValue = method.Invoke(receiver, objects); };

            //bool retval = true;

            executionLog.WriteLine("execute method " + this.method.Name
                + "[" + (this.timesExecuted - 1).ToString() + "] the second time");
            Console.WriteLine("execute method " + this.method.Name
                + "[" + (this.timesExecuted - 1).ToString() + "] the second time");

            executionLog.Flush();


            if (!CodeExecutor.ExecuteReflectionCall(call, debugLog, out exceptionThrown))
            {
                //for exns we can ony add the class to faulty classes when its a guideline violation
                //if (Util.GuidelineViolation(exceptionThrown.GetType()))
                //{

                //    PlanManager.numDistinctContractViolPlans++;

                //    KeyValuePair<MethodBase, Type> k = new KeyValuePair<MethodBase, Type>(this.method, exceptionThrown.GetType());
                //    if (!exnViolatingMethods.ContainsKey(k))
                //    {
                //        PlanManager.numContractViolatingPlans++;
                //        exnViolatingMethods[k] = true;
                //    }

                //    //add this class to the faulty classes
                //    contractExnViolatingClasses[method.DeclaringType] = true;
                //    contractExnViolatingMethods[method] = true;
                //}

                ret = null;
                executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString()
                    + "]: invocationOk is false the sceond time --- shouldn't happen?");

                //this.ReturnValue.Add(null); 

                return false;
            }
            else
            {
                ret = new ResultTuple(method, receiver, returnValue, objects);

                ////xiao.qu@us.abb.com adds to capture return value -- start////
                if (returnValue != null)
                {
                    if ((returnValue.GetType() == typeof(System.String))
                         || (returnValue.GetType() == typeof(System.Boolean))
                         || (returnValue.GetType() == typeof(byte))
                         || (returnValue.GetType() == typeof(short))
                         || (returnValue.GetType() == typeof(int))
                         || (returnValue.GetType() == typeof(long))
                         || (returnValue.GetType() == typeof(float))
                         || (returnValue.GetType() == typeof(double))
                         || (returnValue.GetType() == typeof(char)))
                        executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString() + "] the second time: "
                    + returnValue.ToString().Replace("\n", "\\n").Replace("\r", "\\r"));
                    else
                        executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString() + "] the second time: not primitive or string");

                    //this.ReturnValue.Add(returnValue);
                    Type typeOfReturnVal = returnValue.GetType();
                    if (typeOfReturnVal == typeof(bool) || typeOfReturnVal == typeof(byte) || typeOfReturnVal == typeof(short)
                        || typeOfReturnVal == typeof(int) || typeOfReturnVal == typeof(long) || typeOfReturnVal == typeof(float)
                        || typeOfReturnVal == typeof(double) || typeOfReturnVal == typeof(char) || typeOfReturnVal == typeof(string))
                    {
                        if (returnValue.Equals(retValOldRun))
                            return true;
                        else
                            return false;
                    }

                    return true; //other types just assert NOTNULL -- should be ok

                }
                else
                {
                    executionLog.WriteLine("return value [" + (this.timesExecuted - 1).ToString()
                        + "]: no return value the second time -- shouldn't happen?");

                    //this.ReturnValue.Add(null);
                    return false;
                }
                ////xiao.qu@us.abb.com adds to capture return value -- end////
            }

            //check if the objects in the output tuple violated basic contracts
            //if (ret != null)
            //{
            //    foreach (object o in ret.tuple)
            //    {
            //        if (o == null) continue;

            //        bool toStrViol, hashCodeViol, equalsViol;
            //        int count;
            //        if (Util.ViolatesContracts(o, out count, out toStrViol, out hashCodeViol, out equalsViol))
            //        {
            //            contractViolated = true;
            //            contractExnViolatingMethods[method] = true;

            //            PlanManager.numDistinctContractViolPlans++;

            //            bool newcontractViolation = false;

            //            if (toStrViol)
            //            {

            //                if (!toStrViolatingMethods.ContainsKey(method))
            //                    newcontractViolation = true;
            //                toStrViolatingMethods[method] = true;
            //            }
            //            if (hashCodeViol)
            //            {
            //                if (!hashCodeViolatingMethods.ContainsKey(method))
            //                    newcontractViolation = true;

            //                hashCodeViolatingMethods[method] = true;
            //            }
            //            if (equalsViol)
            //            {
            //                if (!equalsViolatingMethods.ContainsKey(method))
            //                    newcontractViolation = true;

            //                equalsViolatingMethods[method] = true;
            //            }

            //            if (newcontractViolation)
            //                PlanManager.numContractViolatingPlans++;

            //            //add this class to the faulty classes
            //            contractExnViolatingClasses[method.DeclaringType] = true;

            //            retval = false;
            //        }

            //    }
            //}

            //if (contractViolated)  
            //    executionLog.WriteLine("contract violation.");           

            //return retval;
        }

        public override string Namespace
        {
            get
            {
                return this.method.DeclaringType.Namespace;
            }
        }

        public override ReadOnlyCollection<Assembly> Assemblies
        {
            get
            {
                return ReflectionUtils.GetRelatedAssemblies(this.method);
            }
        }
    }

}
