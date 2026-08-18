---
name: ai-agent-learning
description: Explain the core ideas behind AI coding agents, how they work in this repo, and when to use instructions, tools, skills, MCP, and session context.
---

# AI Agent Learning

Use this skill when the user wants to understand how working with an AI coding agent differs from asking a normal chatbot.

## Core ideas

- Instructions: long-lived guidance that tells the agent how to behave in this repo.
- Tools: the actions the agent can perform, such as reading files, editing code, running commands, and inspecting the browser.
- Skills: specialized capabilities for a task, like code review, research, or repo hygiene.
- MCP servers: optional integrations that connect the agent to external systems such as GitHub, databases, shell tools, and browser automation.
- Session context: runtime information such as current repo, active files, branch, and session state.
- Repo conventions: repo-specific rules held in docs and instructions so the agent behaves consistently.

## How to explain it simply

- Chat = general helper
- Agent = specialist helper with tools and limits
- Skill = a focused capability
- Instruction = rules for how to behave
- MCP = connection to outside systems only if allowed

## In this repo

This project is used as a practical learning environment for AI agent workflows. Keep the mental model simple:

1. The repo says what matters.
2. Instructions tell the agent the expected behavior.
3. Skills add specialized tasks.
4. Tools let the agent actually do the work.
5. MCP adds access to external systems when configured.
6. Session context keeps the agent grounded in the current task.

## Good usage pattern

Use this skill when the user asks:
- what is an agent
- what is a skill
- what is an MCP server
- what is session context
- why repo instructions matter
- how to use AI coding tools in a real project

## Example explanation

"An AI agent is not magic. It is a model plus tools plus instructions plus context. The model decides what to do, tools do the work, instructions constrain the behavior, and the repo gives the rules."

## Practice rule

When explaining agent behavior, keep the answer short and concrete. Prefer:
- what it is
- what it can access
- what it cannot do by default
- where the rules live

Avoid talking about 'AI intelligence' in abstract terms. Focus on the operational setup: tools, permissions, instructions, and repo rules.
