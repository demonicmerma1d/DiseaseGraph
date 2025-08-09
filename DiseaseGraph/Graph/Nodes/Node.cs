using System.Security.Cryptography.X509Certificates;
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
    public struct NodeParams(double timeStep,double baseInfectChance)//simplifies base graph code for adding simulation parameters
    {
        public double BaseInfectChance = baseInfectChance;
        public double TimeStep = timeStep;
        public double IsolateChance = 0;
        public bool HasReinfectionTimer = false;
        public double DeathChance = 1;
    }
    public class Node //generic infection node type
    {
        protected double InfectionTime;
        protected double TimeStep;
        public bool ChangeState;
        public double ViralLoad;
        public double BaseInfectChance;
        public bool MarkedAsInfected;
        public double ReinfectionTime;
        public bool HasReinfectionTimer;
        public double IsolateChance;
        public double DeathChance;
        public double TimeUntilIsolation;
        public bool WillIsolate;
        public bool IsIsolating;
        protected double Delay; //delay for symptoms showing, usable for case of symptoms = infectious(an incubation period)
        public NodeState NodeState {get; protected set;}
        public NodeState OldNodeState { get;  protected set;}
        public bool IsAlive { get { return AliveCheck(); } }
        protected bool WillDieOnInfection;
        public Node(){}
        protected Node(NodeParams nodeParams)
        {
            TimeStep = nodeParams.TimeStep;
            InfectionTime = 0;
            ViralLoad = 0;
            OldNodeState = NodeState.Susceptible;
            NodeState = NodeState.Susceptible;
            ChangeState = false;
            BaseInfectChance = nodeParams.BaseInfectChance;
            MarkedAsInfected = false;
            Delay = 0;
            ReinfectionTime = 0;
            TimeUntilIsolation = 0;
            HasReinfectionTimer = nodeParams.HasReinfectionTimer;
            IsolateChance = nodeParams.IsolateChance;
            WillIsolate = false;
            IsIsolating = false;
            WillDieOnInfection = true; //hardcoded default behaviour, more complicated cases handled reusing the viral load upon infection as a pseudorandom
        }
        public override string ToString()
        {
            return $"{OldNodeState} : {NodeState} : {IsAlive}";
        }
        public virtual Node Create(NodeParams nodeParams)
        {
            return new(nodeParams);
        }
        protected bool AliveCheck() => !WillDieOnInfection || NodeState != NodeState.Removed;
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
                case NodeState.Removed:
                    if (WillDieOnInfection) break;
                    if (HasReinfectionTimer) NodeState = NodeState.Susceptible;
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
            if (NodeState == NodeState.Removed) return NodeState;
            InfectionTime-=TimeStep;
            if (InfectionTime <= 0 && NodeState == NodeState.Infectious)
            {
                InfectionTime = 0;
                AdvanceState();
            }
            return NodeState;
        }
        public virtual void Infect(RunParams runParams, double viralLoad)
        {
            if (NodeState != NodeState.Susceptible) throw new Exception($"Only Susceptible nodes can be infected, this node is {NodeState}");
            AdvanceState();
            InfectionTime = runParams.InfectionTime;
            Delay = runParams.Delay;
            ViralLoad = viralLoad;
            ReinfectionTime = runParams.ReinfectionTime;
        }
        public void UpdateTimeStep(double newTimeStep)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(newTimeStep, $"{newTimeStep} must be greater or equal to zero.");
            TimeStep = newTimeStep;
        }
    }
}