using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StateMachine<StateEnum, EventEnum>
{
	public struct Transition
	{
		public EventEnum transEvent;
		public StateEnum destState;
		public System.Action action;
	}

	Dictionary<StateEnum, Dictionary<EventEnum, Transition>> _transitions = new Dictionary<StateEnum, Dictionary<EventEnum, Transition>>();
	Dictionary<StateEnum, System.Action> _enterActions = new Dictionary<StateEnum, System.Action>();
	Dictionary<StateEnum, System.Action> _exitActions = new Dictionary<StateEnum, System.Action>();

	public StateEnum CurrentState;

	public void AddTransition(StateEnum origState, EventEnum transEvent, StateEnum destState, System.Action action)
	{
		if (!_transitions.ContainsKey(origState))
		{
			_transitions[origState] = new Dictionary<EventEnum, Transition>();
		}

		_transitions[origState][transEvent] = new Transition { transEvent = transEvent, destState = destState, action = action }; 
	}

	// will be executed after any regular transition action
	public void AddEnterAction(StateEnum destState, System.Action action)
	{
		_enterActions[destState] = action;
	}

	// will be executed before any regular transition action
	public void AddExitAction(StateEnum origState, System.Action action)
	{
		_exitActions[origState] = action;
	}

	public void RaiseEvent(EventEnum transEvent)
	{
		if (!_transitions.ContainsKey(CurrentState))
		{
			// this state has no transition events
			return;
		}

		if (!_transitions[CurrentState].ContainsKey(transEvent))
		{
			// this state doesn't have this transition event
			return;
		}

		Transition transition = _transitions[CurrentState][transEvent];

		// exit state
		{
			if (_exitActions.ContainsKey(CurrentState) && _exitActions[CurrentState] != null)
			{
				_exitActions[CurrentState].Invoke();
			}
		}

		if (transition.action != null)
		{
			transition.action.Invoke();
		}

		// enter state
		{
			CurrentState = transition.destState;

			if (_enterActions.ContainsKey(CurrentState) && _enterActions[CurrentState] != null)
			{
				_enterActions[CurrentState].Invoke();
			}
		}
	}
}
