using FastDeepCloner;
using Microsoft.VisualBasic;
namespace DiseaseGraph.Graph
{
    public enum NodeState
    {
        Susceptible,
        Exposed,
        Infectious,
        Removed,
        Dead
    }
    public readonly struct StaticNodeData<TNode>(TNode node) where TNode : Node
    {
        public readonly NodeState OldNodeState = node.OldNodeState;
        public readonly NodeState NodeState = node.NodeState;
        public readonly bool IsAlive = node.IsAlive;
    }
    public class Node //generic infection node type
    {
        public double InfectionTime;
        protected double TimeStep;
        public bool ChangeState;
        public double ViralLoad;
        public double BaseInfectChance;
        public bool MarkedAsInfected;
        protected double Delay; //delay for symptoms showing, usable for case of symptoms = infectious(an incubation period)
        public NodeState NodeState {get; protected set;}
        public NodeState OldNodeState { get;  protected set;}
        public bool IsAlive { get { return AliveCheck(); } }
        public Node(){}
        protected Node(double timeStep,double baseInfectChance, double infectionTime = 0,double incubationTime = 0)
        {
            InfectionTime = infectionTime;
            TimeStep = timeStep;
            ViralLoad = 0;
            OldNodeState = NodeState.Susceptible;
            NodeState = NodeState.Susceptible;
            ChangeState = false;
            BaseInfectChance = baseInfectChance; 
            MarkedAsInfected = false;
            Delay = incubationTime;
        }
        public override string ToString()
        {
            return $"{OldNodeState} : {NodeState} : {IsAlive}";
        }
        public virtual Node Create(params double[] args) //timestep,infection chance,death chance as 3rd?
        {
            if (args.Length < 2) throw new ArgumentException($"{args.Length} values given,at least 2 are required");
            return new(args[0],args[1]);
        }
        protected bool AliveCheck() => NodeState != NodeState.Removed; //for non reinfectible purposes, functionally Removed = dead
        protected virtual void AdvanceState()
        {
            ChangeState = true;
            OldNodeState = NodeState;
            switch (NodeState)
            {
                case NodeState.Susceptible:
                    NodeState = NodeState.Infectious;
                    break;
                case NodeState.Infectious:
                    NodeState = NodeState.Removed;
                    break;
                default:
                    break;
            }
        }
        public void Reset()
        {
            OldNodeState = NodeState.Susceptible;
            NodeState = NodeState.Susceptible;
        }
        public virtual double GetViralLoad(double infectionThreshold,double infectionCall,double baseViralLoad)
        {
            return baseViralLoad;
        }
        public virtual NodeState Update()
        {
            if (NodeState == NodeState.Removed || InfectionTime == 0) return NodeState;
            InfectionTime-=TimeStep;
            if (InfectionTime <= 0)
            {
                InfectionTime = 0;
                AdvanceState();
            }
            return NodeState;
        }
        public virtual void Infect(double infectionTime, double incubationTime, double viralLoad)
        {
            AdvanceState();
            InfectionTime = infectionTime;
            Delay = incubationTime;
            ViralLoad = viralLoad;
        }
        public void UpdateTimeStep(double newTimeStep)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(newTimeStep, $"{newTimeStep} must be greater or equal to zero.");
            TimeStep = newTimeStep;
        }
    }
}