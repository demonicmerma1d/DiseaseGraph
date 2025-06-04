namespace DiseaseGraph.Graph
{
    public class IncubateNode : Node
    {
        public IncubateNode(){}
        protected IncubateNode(double timeStep, double baseInfectChance, double infectionTime = 0, double incubationTime = 0)
            : base(timeStep, baseInfectChance, infectionTime, incubationTime) { }
        protected override void AdvanceState()
        {
            ChangeState = true;
            OldNodeState = NodeState;
            switch (NodeState)
            {
                case NodeState.Susceptible:
                    NodeState = NodeState.Exposed;
                    break;
                case NodeState.Exposed:
                    NodeState = NodeState.Infectious;
                    break;
                default:
                    base.AdvanceState();
                    break;
            }
        }
        public override NodeState Update()
        {
            if (NodeState == NodeState.Removed) return NodeState;
            if (Delay <= 0)
            {
                Delay = 0;
                if (InfectionTime <= 0)
                {
                    InfectionTime = 0;
                    AdvanceState();
                    return NodeState;
                }
                if (NodeState == NodeState.Exposed) AdvanceState(); 
                InfectionTime -= TimeStep;
                return NodeState;
            }
            else Delay -= TimeStep;
            return NodeState;
        }
        public override IncubateNode Create(params double[] args)
        {
            if (args.Length < 2) throw new ArgumentException($"{args.Length} values given, at least 2 values are required");
            return new(args[0],args[1]);
        }
    }
}