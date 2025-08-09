namespace DiseaseGraph.Graph
{
    public class IncubateSSNode : IncubateNode
    {
        public IncubateSSNode(){}
        protected IncubateSSNode(NodeParams nodeParams) : base(nodeParams) { }
        public override IncubateSSNode Create(NodeParams nodeParams)
        {
            return new(nodeParams);
        }
        public override double GetViralLoad(double infectionThreshold, double infectionCall, double baseViralLoad)
        {
            return infectionCall / ((infectionThreshold < 0.2 ? 3 : 1) * baseViralLoad); //something more complicated could be done, I dont feel like it
        }
    }
}