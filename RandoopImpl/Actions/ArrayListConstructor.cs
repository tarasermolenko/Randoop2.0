﻿using Common;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace Randoop
{
    public class ArrayListBuilderTransformer : ArrayOrArrayListBuilderTransformer
    {
        private static Dictionary<BaseTypeAndLengthPair, ArrayListBuilderTransformer> cachedTransformers =
          new Dictionary<BaseTypeAndLengthPair, ArrayListBuilderTransformer>();

        public static ArrayListBuilderTransformer Get(Type arrayBaseType, int arrayLength)
        {
            ArrayListBuilderTransformer t;
            BaseTypeAndLengthPair p = new BaseTypeAndLengthPair(arrayBaseType, arrayLength);
            cachedTransformers.TryGetValue(p, out t);
            if (t == null)
                cachedTransformers[p] = t = new ArrayListBuilderTransformer(arrayBaseType, arrayLength);
            return t;
        }

        public override int TupleIndexOfIthInputParam(int i)
        {
            throw new NotImplementedException("Operation not supported (no input parameters in tuple)");
        }

        public override Type[] TupleTypes
        {
            get { return new Type[] { typeof(ArrayList) }; }
        }

        public override bool[] DefaultActiveTupleTypes
        {
            get { return new bool[] { true }; }
        }

        public override bool Equals(object obj)
        {
            ArrayListBuilderTransformer t = obj as ArrayListBuilderTransformer;
            if (t == null)
                return false;
            return (this.baseType.Equals(t.baseType) && this.length == t.length);
        }

        public override int GetHashCode()
        {
            return this.baseType.GetHashCode() + length.GetHashCode();
        }

        public override string ToString()
        {
            return "ArrayList of " + baseType.FullName + " of length " + length;
        }

        private ArrayListBuilderTransformer(Type arrayBaseType, int arrayLength)
            : base(arrayBaseType, arrayLength)
        {
        }

        public override string ToCSharpCode(ReadOnlyCollection<string> arguments, String newValueName)
        {
            Util.Assert(arguments.Count == this.ParameterTypes.Length);

            StringBuilder b = new StringBuilder();
            string retType = "System.Collections.ArrayList";
            b.Append(retType + " " + newValueName + " =  new " + retType + "();\n\r");

            for (int i = 0; i < arguments.Count; i++)
            {
                b.Append(newValueName + "." + "Add(" + arguments[i] + "); \n\r");
            }
            return b.ToString();
        }

        ////xiao.qu@us.abb.com adds for capture return value for regression assertion
        //public override string ToCSharpCode(ReadOnlyCollection<string> arguments, string newValueName, string return_val)
        //{
        //    return ToCSharpCode(arguments, newValueName);
        //}

        public override bool Execute(out ResultTuple ret, ResultTuple[] parameters,
            Plan.ParameterChooser[] parameterMap, TextWriter executionLog, TextWriter debugLog, out Exception exceptionThrown, out bool contractViolated, bool forbidNull)
        {
            contractViolated = false;

            ArrayList a = new ArrayList();

            for (int i = 0; i < length; i++)
            {

                Plan.ParameterChooser pair = parameterMap[i];

                if (forbidNull)
                    Util.Assert(parameters[pair.planIndex].tuple[pair.resultIndex] != null);

                a.Add(parameters[pair.planIndex].tuple[pair.resultIndex]);
            }

            exceptionThrown = null;
            ret = new ResultTuple(this, a);
            executionLog.WriteLine("execute arraylistconstructor type " + a.GetType().ToString());//xiao.qu@us.abb.com adds
            return true;
        }

        public override string Namespace
        {
            get
            {
                return this.baseType.Namespace;
            }
        }

        public override ReadOnlyCollection<Assembly> Assemblies
        {
            get
            {
                return ReflectionUtils.GetRelatedAssemblies(this.baseType);
            }
        }
    }

}
