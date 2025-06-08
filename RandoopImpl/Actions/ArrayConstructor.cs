﻿using Common;
using System;

using System;
using System.Collections;
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

    public class ArrayBuilderTransformer : ArrayOrArrayListBuilderTransformer
    {

        private static Dictionary<BaseTypeAndLengthPair, ArrayBuilderTransformer> cachedTransformers =
            new Dictionary<BaseTypeAndLengthPair, ArrayBuilderTransformer>();

        public static ArrayBuilderTransformer Get(Type arrayBaseType, int arrayLength)
        {
            ArrayBuilderTransformer t;
            BaseTypeAndLengthPair p = new BaseTypeAndLengthPair(arrayBaseType, arrayLength);
            cachedTransformers.TryGetValue(p, out t);
            if (t == null)
                cachedTransformers[p] = t = new ArrayBuilderTransformer(arrayBaseType, arrayLength);
            return t;
        }


        public override string MemberName
        {
            get
            {
                return "n/a";
            }
        }


        public override int TupleIndexOfIthInputParam(int i)
        {
            throw new NotImplementedException("Operation not supported (no input parameters in tuple)");
        }


        public override Type[] TupleTypes
        {
            get { return new Type[] { baseType.MakeArrayType(1) }; }
        }

        public override bool[] DefaultActiveTupleTypes
        {
            get { return new bool[] { true }; }
        }

        public override bool Equals(object obj)
        {
            ArrayBuilderTransformer t = obj as ArrayBuilderTransformer;
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
            return "array of " + baseType.FullName + " of length " + length;
        }

        private ArrayBuilderTransformer(Type arrayBaseType, int arrayLength)
            : base(arrayBaseType, arrayLength)
        {
        }

        public override string ToCSharpCode(ReadOnlyCollection<string> arguments, String newValueName)
        {
            Util.Assert(arguments.Count == this.ParameterTypes.Length);

            StringBuilder b = new StringBuilder();
            string retType = this.baseType + "[]";
            b.Append(retType + " " + newValueName + " =  new " + retType + " {");

            for (int i = 0; i < arguments.Count; i++)
            {
                if (i > 0) b.Append(" , ");
                b.Append(arguments[i]);
            }
            b.Append("};");
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

            Array array;

            try
            {
                array = Array.CreateInstance(baseType, length);
            }
            catch (Exception e)
            {
                ret = null;
                exceptionThrown = e;
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                Plan.ParameterChooser pair = parameterMap[i];

                if (forbidNull)
                    Util.Assert(parameters[pair.planIndex].tuple[pair.resultIndex] != null);

                array.SetValue(parameters[pair.planIndex].tuple[pair.resultIndex], i);
            }

            ret = new ResultTuple(this, array);
            executionLog.WriteLine("create array (constructor): type " + array.GetType().ToString()); //xiao.qu@us.abb.com adds
            exceptionThrown = null;
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
