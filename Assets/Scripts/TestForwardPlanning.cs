﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Terrapass.GameAi.Goap.Planning;
using Terrapass.GameAi.Goap.Planning.Preconditions;
using Terrapass.GameAi.Goap.Planning.Effects;
using Terrapass.Time;

public class TestForwardPlanning : MonoBehaviour
{
	void Start()
	{
		var currentWorldState = new WorldState.Builder()
			.SetSymbol(new SymbolId("Wood"), 0)
			.SetSymbol(new SymbolId("Stone"), 2)
			.SetSymbol(new SymbolId("Iron"), 0)
			.SetSymbol(new SymbolId("HouseBuilt"), 0)
			.SetSymbol(new SymbolId("WoodInStorage"), 5)
			.SetSymbol(new SymbolId("StoneInStorage"), 3)
			.SetSymbol(new SymbolId("IronInStorage"), 0)
			.SetSymbol(new SymbolId("HasAxe"), 0)
			.SetSymbol(new SymbolId("AxesAvailable"), 1)
			.Build();

		var availableActions = new List<PlanningAction>() {
			new PlanningAction(
				"BuildHouse",
				new List<IPrecondition>() {
					new IsNotSmaller(new SymbolId("Wood"), 20),
					new IsNotSmaller(new SymbolId("Stone"), 5)
				},
				new List<IEffect>() {
					new SetTrue(new SymbolId("HouseBuilt")),
					new Subtract(new SymbolId("Wood"), 20),
					new Subtract(new SymbolId("Stone"), 5)
				},
				30
			),
			new PlanningAction(
				"GetWoodFromStorage",
				new List<IPrecondition>() {
					new IsNotSmaller(new SymbolId("WoodInStorage"), 1)
				},
				new List<IEffect>() {
					new Add(new SymbolId("Wood"), 1),
					new Subtract(new SymbolId("WoodInStorage"), 1),
				},
				2
			),
			new PlanningAction(
				"GetStoneFromStorage",
				new List<IPrecondition>() {
					new IsNotSmaller(new SymbolId("StoneInStorage"), 1)
				},
				new List<IEffect>() {
					new Add(new SymbolId("Stone"), 1),
					new Subtract(new SymbolId("StoneInStorage"), 1),
				},
				3
			),
			new PlanningAction(
				"GetIronFromStorage",
				new List<IPrecondition>() {
					new IsNotSmaller(new SymbolId("IronInStorage"), 1)
				},
				new List<IEffect>() {
					new Add(new SymbolId("Iron"), 1),
					new Subtract(new SymbolId("IronInStorage"), 1),
				},
				2
			),
			new PlanningAction(
				"CutTrees",
				new List<IPrecondition>() {
					new IsTrue(new SymbolId("HasAxe"))
				},
				new List<IEffect>() {
					new Add(new SymbolId("Wood"), 8)//,
					//new SetFalse(new SymbolId("HasAxe"))
				},
				5
			),
			new PlanningAction(
				"TakeAxe",
				new List<IPrecondition>() {
					new IsFalse(new SymbolId("HasAxe")),
					new IsNotSmaller(new SymbolId("AxesAvailable"), 1)
				},
				new List<IEffect>() {
					new SetTrue(new SymbolId("HasAxe")),
					new Subtract(new SymbolId("AxesAvailable"), 1)
				},
				1
			),
			new PlanningAction(
				"MakeAxe",
				new List<IPrecondition>() {
					new IsFalse(new SymbolId("HasAxe")),
					new IsNotSmaller(new SymbolId("Wood"), 2),
					new IsNotSmaller(new SymbolId("Iron"), 3)
				},
				new List<IEffect>() {
					new SetTrue(new SymbolId("HasAxe")),
					new Subtract(new SymbolId("Wood"), 2),
					new Subtract(new SymbolId("Iron"), 3)
				},
				15
			)
		};

		var buildHouseGoal = new Goal(
			"BuildHouse",
			new List<IPrecondition>() {
				new IsTrue(new SymbolId("HouseBuilt"))
			}
       	);

		var planner = new ForwardPlanner();

		try
		{
			var timer = new SystemExecutionTimer();
			var plan = planner.FormulatePlan(currentWorldState, availableActions, buildHouseGoal);
			print(
				string.Format(
					"{0} has found a plan of length {1} and cost {2} to satisfy \"{3}\" in {4} seconds",
					planner.GetType(), 
					plan.Length,
					plan.Cost,
					buildHouseGoal.Name,
					timer.ElapsedSeconds
				)
			);
			print(plan.ToString());
			TestUtils.ValidatePlan(plan, currentWorldState, buildHouseGoal);
		}
		catch(PlanNotFoundException e)
		{
			Debug.LogWarningFormat("{0}: {1}", e.Message, e.InnerException.Message);
		}
	}
}

