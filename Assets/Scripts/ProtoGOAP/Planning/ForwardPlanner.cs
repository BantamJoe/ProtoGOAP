﻿using System;
using System.Linq;
using System.Collections.Generic;

using Terrapass.Debug;

using ProtoGOAP.Graphs;

namespace ProtoGOAP.Planning
{
	using Internal;

	public class ForwardPlanner : IPlanner
	{
		public const int DEFAULT_MAX_PLAN_LENGTH = 20;

		private readonly int maxPlanLength;
		private readonly IPathfinder<ForwardNode> pathfinder;

		public ForwardPlanner(int maxPlanLength = DEFAULT_MAX_PLAN_LENGTH)
		{
			if(maxPlanLength <= 0)
			{
				throw new ArgumentException("maxPlanLength must be positive", "maxPlanLength");
			}
			this.maxPlanLength = maxPlanLength;
			this.pathfinder = new AstarPathfinder<ForwardNode>(
				new AstarPathfinderConfiguration<ForwardNode>.Builder()
					.UseHeuristic(PathfindingHeuristic)
					.LimitSearchDepth(maxPlanLength)
					.LimitSearchTime(0.35)
					//.LimitSearchTime(1)
					//.AssumeNonNegativeCosts()
					.Build()
			);
		}

		#region IPlanner implementation

		public Plan FormulatePlan(
			IKnowledgeProvider knowledgeProvider, 
			IEnumerable<PlanningAction> availableActions, 
			Goal goal
		)
		{
			// TODO: Ideally, world state should be populated lazily, not in advance, like here.
			// Some symbols may not be needed at all to build a plan!
			// (also see comments on IPrecondition and IEffect RelevantActions property)
			WorldState initialWorldState = new WorldState();

			availableActions.Aggregate(new List<SymbolId>(), (soFar, action) => {soFar.AddRange(action.GetRelevantSymbols()); return soFar;})
				.ForEach((symbolId) => {
					if(!initialWorldState.Contains(symbolId))
					{
						initialWorldState = initialWorldState.BuildUpon()
							.SetSymbol(symbolId, knowledgeProvider.GetSymbolValue(symbolId)).Build();
					}
				});

			foreach(SymbolId symbolId in goal.PreconditionSymbols)
			{
				if(!initialWorldState.Contains(symbolId))
				{
					initialWorldState = initialWorldState.BuildUpon()
						.SetSymbol(symbolId, knowledgeProvider.GetSymbolValue(symbolId)).Build();
				}
			}

			// TODO: After lazy population is implemented, the following part of the method is the only thing
			// that must remain.
			try
			{
				var path = this.pathfinder.FindPath(
					ForwardNode.MakeRegularNode(initialWorldState, new ForwardNodeExpander(availableActions)),
					ForwardNode.MakeGoalNode(goal)
	           	);

				// FIXME: Downcasting to ForwardEdge - may be fixed by adding another generic parameter to IPathfinder.
				return new Plan(from edge in path.Edges select ((ForwardEdge)edge).Action.Name);
			}
			catch(PathNotFoundException e)
			{
				throw new PlanNotFoundException(this, maxPlanLength, goal, e);
			}
		}

		#endregion

		private static double PathfindingHeuristic(ForwardNode sourceNode, ForwardNode targetNode)
		{
			DebugUtils.Assert(!sourceNode.IsGoal, "sourceNode must be a regular (non-goal) {1}", typeof(ForwardNode));
			DebugUtils.Assert(targetNode.IsGoal, "targetNode must be a goal {0}", typeof(ForwardNode));

			// TODO: This heuristic is not particularly effective.
			// See if a better heuristic for forward planning can be found.
			return targetNode.Goal.GetDistanceFrom(sourceNode.WorldState);
		}
	}
}

