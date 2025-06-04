namespace DiseaseGraph.Graph
{
    public class IncubateSSNode : IncubateNode
    {
        public IncubateSSNode(){}
        protected IncubateSSNode(double timeStep, double baseInfectChance, double infectionTime = 0, double incubationTime = 0)
            : base(timeStep, baseInfectChance, infectionTime, incubationTime) { }
        public override IncubateSSNode Create(params double[] args)
        {
            if (args.Length < 2) throw new ArgumentException($"{args.Length} values given, at least 2 values are required");
            return new(args[0],args[1]);
        }
        public override double GetViralLoad(double infectionThreshold, double infectionCall, double baseViralLoad)
        {
            return infectionCall / ((infectionThreshold < 0.2 ? 3 : 1) * baseViralLoad); //something more complicated could be done, I dont feel like it
        }
    }
}