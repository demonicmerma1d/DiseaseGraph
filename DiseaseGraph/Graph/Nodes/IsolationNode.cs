namespace DiseaseGraph.Graph
{
    public class IsolationNode : Node
    {
        public IsolationNode(){}
        protected IsolationNode(NodeParams nodeParams)
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
        public override NodeState Update() //switch statement!!!
        {
            if (WillIsolate && !IsIsolating)
            {
                if (TimeUntilIsolation <= 0)
                {
                    TimeUntilIsolation = 0;
                    IsIsolating = true;
                }
                else TimeUntilIsolation -= TimeStep;
            }
            switch (NodeState)
            {
                case NodeState.Exposed:
                    if (Delay <= 0)
                    {
                        Delay = 0;
                        AdvanceState();
                        break;
                    }
                    Delay -= TimeStep;
                    break;
                case NodeState.Infectious:
                    if (InfectionTime <= 0)
                    {
                        InfectionTime = 0;
                        AdvanceState();
                        IsIsolating = false;
                        WillIsolate = false;
                        break;
                    }
                    InfectionTime -= TimeStep;
                    break;
                case NodeState.Removed:
                    if (!HasReinfectionTimer) break;
                    if (ReinfectionTime <= 0)
                    {
                        ReinfectionTime = 0;
                        AdvanceState();
                        break;
                    }
                    ReinfectionTime -= TimeStep;
                    break;
                default:
                    break;
            }
            return NodeState;
        }
        public override IsolationNode Create(NodeParams nodeParams)
        {
            return new(nodeParams);
        }
    }
}