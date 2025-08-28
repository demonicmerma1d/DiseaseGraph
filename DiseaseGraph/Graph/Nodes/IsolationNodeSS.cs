namespace DiseaseGraph.Graph
{
    public class IsolationNodeSS : IsolationNode
    {
        public IsolationNodeSS(){}
        protected IsolationNodeSS(NodeParams nodeParams)
            : base(nodeParams) { }
        public override double GetViralLoad(double infectionThreshold, double infectionCall, double baseViralLoad)
        {
            return ((infectionCall / infectionThreshold) < 0.2 ? 3 : 1) * baseViralLoad; //something more complicated could be done, I dont feel like it
        }
        public override IsolationNodeSS Create(NodeParams nodeParams)
        {
            return new(nodeParams);
        }
    }
}