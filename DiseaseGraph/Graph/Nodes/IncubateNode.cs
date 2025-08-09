namespace DiseaseGraph.Graph
{
    public class IncubateNode : Node
    {
        public IncubateNode(){}
        protected IncubateNode(NodeParams nodeParams)
            : base(nodeParams) { }
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
        public override IncubateNode Create(NodeParams nodeParams)
        {
            return new(nodeParams);
        }
    }
}