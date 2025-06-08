using System.Reflection;
using System.Collections.ObjectModel;
using Common.RandoopBareExceptions;
using Common;
using Randoop;

namespace RandoopBare
{

    // TO DO: Rework this to be compatible with .NET 8
    // realiant on RandoopCommon, so need to get that sorted first

    class MainMethod
    {
        static void Main(string[] args)
        {
            
            try
            {
                Main2(args);
            }
            catch (InvalidUserParamsException e)
            {
                Console.Error.WriteLine(e.Message);
                System.Environment.Exit(1);
            }
            catch (InternalError e)
            {
                Console.Error.WriteLine(Util.SummarizeException(e, Common.Environment.RandoopBareInternalErrorMessage));
                System.Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(Util.SummarizeException(e, ""));
                System.Environment.Exit(1);
            }
        }

        private static void Main2(string[] args)
        {
            if (args.Length != 1)
            {
                throw new InvalidUserParamsException(
                    "RandoopBare takes exactly one argument but was "
                    + "given the following arguments:"
                    + System.Environment.NewLine
                    + Util.PrintArray(args)); ;
            }

            // Parse XML file with generation parameters.
            String configFileName = Common.ConfigFileName.Parse(args[0]);
            RandoopConfiguration config = LoadConfigFile(configFileName);

            // Set the random number generator.
            if (config.randomSource == RandomSource.SystemRandom)
            {
                Console.WriteLine("Randoom seed = " + config.randomseed);
                Common.SystemRandom random = new Common.SystemRandom();
                random.Init(config.randomseed);
                Common.Environment.Random = random;
            }
            else
            {
                Util.Assert(config.randomSource == RandomSource.Crypto);
                Console.WriteLine("Randoom seed = new System.Security.Cryptography.RNGCryptoServiceProvider()");
                Common.Environment.Random = new CryptoRandom();
            }

            if (!Directory.Exists(config.outputdir))
                throw new Common.RandoopBareExceptions.InvalidUserParamsException("output directory does not exist: "
                    + config.outputdir);

            Collection<Assembly> assemblies = Common.Misc.LoadAssemblies(config.assemblies);

            ////xiao.qu@us.abb.com for substituting MessageBox.Show() - start
            ////Instrument instrumentor = new Instrument();
            //foreach (FileName asm in config.assemblies)
            //{
            //    Instrument.MethodInstrument(asm.fileName, "System.Windows.Forms.MessageBox::Show", "System.Console.Writeline");
            //}            
            ////xiao.qu@us.abb.com for substituting MessageBox.Show() - end

            IReflectionFilter filter1 = new VisibilityFilter(config);
            ConfigFilesFilter filter2 = new ConfigFilesFilter(config);

            Console.WriteLine("========== REFLECTION PATTERNS:");
            filter2.PrintFilter(Console.Out);

            IReflectionFilter filter = new ComposableFilter(filter1, filter2);

            Collection<Type> typesToExplore = ReflectionUtils.GetExplorableTypes(assemblies);

            PlanManager planManager = new PlanManager(config);
            planManager.builderPlans.AddEnumConstantsToPlanDB(typesToExplore);
            planManager.builderPlans.AddConstantsToTDB(config);

            Console.WriteLine("========== INITIAL PRIMITIVE VALUES:");
            planManager.builderPlans.PrintPrimitives(Console.Out);

            StatsManager stats = new StatsManager(config);

            Console.WriteLine("Analyzing assembly.");

            ActionSet actions = null;
            try
            {
                actions = new ActionSet(typesToExplore, filter);
            }
            catch (EmpytActionSetException)
            {
                string msg = "After filtering based on configuration files, no remaining methods or constructors to explore.";
                throw new Common.RandoopBareExceptions.InvalidUserParamsException(msg);
            }

            Console.WriteLine();
            Console.WriteLine("Generating tests.");

            RandomExplorer explorer =
                new RandomExplorer(typesToExplore, filter, true, config.randomseed, config.arraymaxsize, stats, actions);
            Common.ITimer t = new Common.Timer(config.timelimit);
            try
            {
                explorer.Explore(t, planManager, config.methodweighing, config.forbidnull, true, config.fairOpt);
            }
            catch (Exception e)
            {
                Console.WriteLine("Explorer raised exception {0}", e.ToString());
            }

            //Mono.Cecil.Cil.
        }

        private static RandoopConfiguration LoadConfigFile(string p)
        {
            RandoopConfiguration retval = null;
            try
            {
                retval = RandoopConfiguration.Load(p);
            }
            catch (Exception e)
            {
                string msg = "Failed to load XML configuration file " + p + ": " + e.Message;
                throw new Common.RandoopBareExceptions.InternalError(msg);
            }
            return retval;
        }

    }
}
