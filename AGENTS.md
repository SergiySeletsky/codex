# Migration Plan: Rust Codex CLI to .NET

## Current Status
- Created new .NET console project `codex-dotnet/CodexCli` using .NET 8.
- Introduced `AppConfig` loader using Tomlyn to parse `config.toml`.
- Implemented CLI using System.CommandLine with these features:
  1. Global `--config` option to load configuration.
  2. Global `--cd` option to change working directory.
  3. Interactive mode using Spectre.Console with notification command execution.
  4. `exec` subcommand with model/profile/provider options, images, color enum,
     cwd override, git repo check and config overrides.
  5. `login` subcommand saves token to `~/.codex/token` and reads `CODEX_TOKEN`
     environment variable. Supports config overrides.
  6. `mcp` subcommand starts a basic HTTP listener.
  7. `proto` subcommand that streams stdin lines.
  8. `debug seatbelt` placeholder.
  9. `debug landlock` placeholder.
 10. Interactive mode stores history with `/history`, `/help`, and `/quit`.
 11. Proper subcommand routing with async handlers.
 12. Added simple unit tests for config overrides parsing.
 13. Added Git repo root detection utility.
 14. Implemented elapsed time formatter and tests.
 15. Expanded config loader to support notify arrays and CODEX_HOME env var.
 16. Added approval and sandbox options for exec and interactive commands.
 17. Interactive command now accepts prompt, model and sandbox options.
 18. Added SandboxPermission parser with disk-write-folder support.
 19. Added OpenAI API key manager and extended login command to set it.
 20. Interactive command reads initial prompt from stdin when '-' is used.
21. Added utilities for CODEX_HOME and log directory detection.
22. Added tests for sandbox permission parsing and git repo detection.
23. Introduced simplified protocol events and EventProcessor for printing agent output.
24. Added MockCodexAgent to simulate event streams for exec command.
25. Exec command now prints config summary and processes events, writing last message to file.
26. Interactive mode runs notify command on start and completion and adds /log command.
27. Added NotifyUtils helper and ExitStatus propagation for debug commands.
28. Implemented basic seatbelt/landlock debug commands executing processes.
29. Proto command parses JSON-RPC messages and prints method names.
30. Added timestamp helper in Elapsed and utilities tests.
31. Added EnvUtilsTests verifying CODEX_HOME environment variable handling.
32. Added event and protocol classes under `Protocol/`.
33. Added session manager, /config and /save commands in interactive mode.
34. EnvUtils now supports CODEX_HISTORY_DIR and history directory lookup with tests.
35. Implemented new approval request events and handling in ExecCommand.
36. MockCodexAgent emits approval events; ExecCommand prompts user.
37. Integrated basic OpenAIClient stub in ExecCommand.
38. Added SessionManager tests and OpenAIClient placeholder.
39. Added PatchApply, McpToolCall, AgentReasoning and SessionConfigured events.
40. Extended EventProcessor to display these events.
41. MockCodexAgent now emits diverse events for testing.
42. Interactive command supports provider, color, notify and override options plus /reset command.
43. SessionManager exposes ClearHistory and associated tests.
44. Added EnvUtilsTests for default history dir.
45. SessionManager now writes history entries to files under `GetHistoryDir`.
46. Exec command records agent messages and reports the history file path.
47. Interactive command prints the history file path when exiting.
48. Added `model_provider` support in `AppConfig` and displayed in config summary.
49. Exec command now honors provider from config or CLI.
50. Implemented basic SSE streaming in `mcp` command on `/events`.
51. Added binder unit tests for `ExecBinder` and `InteractiveBinder`.
52. SessionManager tests verify history file creation.
- Build verified with `dotnet build`.
- Tests run with `dotnet test`.

## Files Migrated
- `codex-dotnet/CodexCli` project with Program.cs, command implementations under `Commands/`, and `Config/AppConfig.cs`.

53. Added config profile support with `AppConfig.Load(path, profile)` and `ConfigProfile`.
54. `instructions` field loaded from config or `instructions.md`.
55. Implemented `ProjectDoc` to merge `AGENTS.md` with instructions.
56. Added `--instructions` option plus reasoning effort/summary enums.
57. Exec and interactive commands now apply profiles and load instructions when prompt absent.
58. Added `ReasoningEffort` and `ReasoningSummary` enums with binder support.
59. Implemented session history retrieval via `SessionManager.GetHistory` (existing but now documented).
60. Added tests for profile loading, project doc instructions, and updated binder tests.
- Added hide-agent-reasoning and disable-response-storage settings in config and CLI
61. EventProcessor now accepts a flag to hide reasoning events
62. Config loader parses hide_agent_reasoning and disable_response_storage fields
63. Profiles can override approval_policy and disable_response_storage
64. Exec and interactive commands bind --hide-agent-reasoning and --disable-response-storage options
65. Login command accepts --token and --api-key arguments
66. Binder tests updated for new options
67. AppConfigProfileTests verify new fields
68. Interactive /config command shows reasoning/storage settings
69. EventProcessor prints storage status in summary
70. Added flags to ExecOptions and InteractiveOptions
71. Added --no-project-doc option and CODEX_DISABLE_PROJECT_DOC support
72. ProjectDoc now searches ~/.codex/AGENTS.md
73. Parsed approval_policy and reasoning effort/summary from config and profiles
74. EventProcessor prints reasoning effort and summary
75. Exec command supports --json for raw event output
76. Interactive mode gained /save-last command
77. Added completion subcommand for shell completions
78. Binder tests updated for new options
79. ProjectDoc tests cover environment variable
80. Interactive /config now shows reasoning effort and summary
81. Added ConfigOverrides.Apply to update configuration values
82. Exec and interactive commands apply config overrides at runtime
83. Event processor now auto-detects ANSI color usage
84. Full-auto mode populates sandbox permissions and summary shows sandbox policy
85. EventProcessor prints sandbox in config summary
86. MockCodexAgent accepts image paths and emits upload events; exec command sends images
87. Interactive command supports --output-last-message and writes last message on exit
88. Interactive /config now displays provider information
89. Added unit test for ConfigOverrides.Apply
90. MCP server broadcasts events to multiple SSE clients
- Build verified with `dotnet build`.
- Tests run with `dotnet test`.
91. Added CODEX_LOG_DIR support in EnvUtils and tests
92. SessionManager can list and delete sessions with history command
93. Added /version and /sessions commands in interactive mode
94. New history subcommand with list/show/clear
95. GetLogDir now honors CODEX_LOG_DIR
96. SessionManager tests cover deletion and listing
97. EnvUtils tests cover log directory env variable
98. Interactive /help updated for new commands
99. Program registers history subcommand
100. HistoryCommand prints sessions and history
101. Added global --log-level option and CODEX_LOG_LEVEL env var
102. Version subcommand prints assembly version
103. EventProcessor shows log level in config summary
104. Exec and interactive commands support --event-log to save JSON event log
105. SessionManager records start timestamps and exposes ListSessionsWithInfo
106. History command has path and purge subcommands
107. Interactive /sessions shows start times
108. Interactive /delete removes saved sessions
109. ExecBinder and InteractiveBinder tests updated for new options
110. EnvUtils tests verify log level env var
111. Added AddToHistory and GetHistoryEntry events
112. EventProcessor handles new history events
113. MockCodexAgent emits history events
114. SessionManager can fetch a specific history entry
115. History command supports 'entry' subcommand
116. Login command saves OpenAI API key to auth.json
117. OpenAiKeyManager.SaveKey implemented
118. OpenAIClient sends real requests to api.openai.com
119. SessionManagerTests verify GetHistoryEntry
120. Added Azure.AI.OpenAI package reference
121. Introduced ModelProviderInfo with built-in OpenAI and OpenRouter providers
122. AppConfig parses model_providers and merges with defaults
123. Added GetProvider helper to AppConfig
124. Implemented ApiKeyManager for per-provider API keys
125. Login command accepts --provider and stores keys via ApiKeyManager
126. ExecCommand selects provider info and passes base URL to OpenAIClient
127. OpenAIClient constructor accepts base URL
128. Added unit tests for ModelProviderInfo loading
129. Removed obsolete OpenAiKeyManager
130. Auth.json now stores keys for multiple providers
131. Added built-in providers gemini, ollama, mistral, deepseek, xai and groq
132. ModelProviderInfo includes EnvKeyInstructions
133. AppConfig parses env_key_instructions
134. EnvUtils.GetModelProviderId reads CODEX_MODEL_PROVIDER
135. Exec and login commands honor CODEX_MODEL_PROVIDER
136. Added provider list subcommand
137. Added tests for provider listing and env key instructions
138. Added basic apply_patch parser and PatchApplier library
139. Event definitions updated with FileChange enum
140. ExecCommand now applies patch changes on PatchApplyBeginEvent
141. Added unit tests for patch parser and apply logic
142. MockCodexAgent emits FileChange-based patch event
143. Added provider info and current subcommands
144. Provider login prints EnvKeyInstructions when key missing
145. PatchParser recognizes *** End of File marker
146. Added ParseUnified helper for unified diff support
147. PatchApplier throws PatchParseException on errors
148. Created ApplyPatchCommand to run patches from CLI
149. Program registers apply_patch command
150. ProviderInfoTests verify info subcommand
151. UnifiedDiffTests cover ParseUnified
152. Provider info and unified diff features documented
153. Added apply_patch tool instructions file for reference
154. Implemented ChatGptLogin utility running embedded Python script
155. Login command gains --chatgpt option to launch browser-based login
156. Introduced ExecPolicy loader parsing default.policy
157. ExecCommand enforces ExecPolicy when approving commands
158. PatchApplier validates paths stay within cwd
159. Added ExecPolicyTests and updated ApplyPatchTests
160. Added ProviderInfoTests for EnvKeyInstructions
161. Embedded ChatGptLogin script test
162. Added mock model provider
163. Provider base URL can be overridden via CODEX_MODEL_BASE_URL
164. Introduced RealCodexAgent using OpenAIClient
165. ExecCommand selects RealCodexAgent unless provider is 'mock'
166. ExecPolicy tracks forbidden programs with reasons
167. ExecCommand prints denial reason for forbidden and unverified commands
168. ChatGptLogin throws informative errors on failure
169. ProviderInfoTests cover base URL environment override
170. ExecPolicyTests verify forbidden reason
171. ChatGptLoginTests ensure failure case handled

172. RealCodexAgent streams chat chunks via OpenAIClient.ChatStreamAsync
173. ExecPolicy parses flags and opts from policy file
174. ExecPolicy.VerifyCommand validates option usage
175. ExecCommand uses VerifyCommand for approval checks
176. CODEX_EXEC_POLICY_PATH overrides default policy path
177. ProviderCommand supports add subcommand
178. ProviderCommand supports remove subcommand
179. ProviderCommand supports set-default subcommand
180. ExecPolicyTests cover VerifyCommand
181. ProviderCommandTests cover add/remove/set-default

182. Introduced AffectedPaths summary for patch application
183. PatchApplier.ApplyWithSummary returns affected paths and summary
184. ApplyPatchCommand supports --summary option
185. Improved unified diff algorithm when applying updates
186. ApplyPatchTests verify summary output
187. ApplyPatchTests verify update file patching
188. Added DiffPlex dependency
189. ApplyPatchCommand prints summary when applying patch
190. New unit tests cover improved diff logic
191. Documented apply patch improvements
192. Added ApplyPatchCommandParser for detecting apply_patch CLI arguments
193. Added ApplyPatchAction and ApplyPatchFileChange definitions
194. Implemented MaybeParseApplyPatch and MaybeParseApplyPatchVerified helpers
195. Added heredoc extraction logic for bash -lc patterns
196. ApplyPatchCommandParserTests verify literal and heredoc parsing

197. ExecCommand detects apply_patch commands and applies patches locally using PatchApplier

198. Introduced ShellEnvironmentPolicyInherit enum and EnvironmentVariablePattern glob helper
199. Added ShellEnvironmentPolicy class
200. Implemented ExecEnv.Create to build filtered environments
201. AppConfig parses [shell_environment_policy] table
202. Added ShellEnvironmentPolicy property to AppConfig
203. DebugCommand applies ShellEnvironmentPolicy when running processes
204. Added ExecEnvTests verifying default excludes are removed
205. ExecEnvTests verify include_only filtering
206. Created new test file ExecEnvTests.cs
207. Added CLI options to override shell environment policy
208. Created ShellEnvPolicy fields in ExecOptions and InteractiveOptions
209. Extended ExecBinder and InteractiveBinder to bind env options
210. ExecCommand and InteractiveCommand apply policy overrides and pass env to NotifyUtils
211. NotifyUtils accepts environment dictionary
212. ChatGptLogin accepts environment and LoginCommand uses policy overrides via new binder
213. DebugCommand supports env policy options for seatbelt/landlock
214. Added LoginBinder and LoginOptions classes
215. Updated binder tests for new options
216. Build and tests updated for .NET 8
217. Added HistoryPersistence, UriBasedFileOpener and Tui parsing in AppConfig
218. SessionManager respects HistoryPersistence setting
219. Exec and Interactive commands configure SessionManager persistence
220. Added TaskStartedEvent and EventProcessor handling
221. MockCodexAgent emits TaskStartedEvent
222. History command gained info subcommand listing sessions with start times
223. Added AppConfigExtraTests for new config fields
224. Introduced MarkdownUtils with citation rewriting
225. EventProcessor rewrites file citations in agent messages
226. ExecCommand passes file opener to EventProcessor
227. Added MarkdownUtilsTests verifying citation rewriting

228. Added MessageHistory utility writing global `history.jsonl`
229. AppendEntryAsync sets owner-only permissions on Unix
230. Implemented HistoryMetadataAsync and LookupEntry helpers
231. ExecCommand saves agent messages to message history
232. ExecCommand saves AddToHistory events to message history
233. HistoryCommand now has messages-meta and messages-entry subcommands
234. Added MessageHistoryTests verifying append and lookup
235. InteractiveCommand toggles mouse capture based on config
236. HistoryCommand registers new message history subcommands
237. Installed .NET 8 SDK via apt-get for building
238. Added MessageHistory API for counting, searching, last-N, and clearing entries
239. Extended HistoryCommand with messages-path, messages-clear, messages-search,
     messages-last and messages-count subcommands
240. MessageHistoryTests cover new APIs

241. Implemented minimal McpClient class for JSON-RPC communication
242. Added mcp-client command running an MCP server and listing tools
243. Added JsonToToml utility converting JSON values to TOML
244. Introduced JsonToTomlTests verifying basic conversion
245. Provider command now supports 'login' subcommand to store API keys
246. Program registers new mcp-client command
247. History messages-last subcommand accepts --json option
248. Added unit tests for JsonToToml
249. Created McpClientCommand options for timeout and JSON output
250. Implemented provider login key saving via ApiKeyManager
251. Added typed MCP request/response records and default server environment
252. McpClient now exposes InitializeAsync, ListToolsAsync and CallToolAsync
253. McpClientCommand supports --call, --args and --env for tool invocation
254. McpClientCommand prints results in JSON format when requested
255. Added CreateServerEnv helper aligning with Rust defaults
256. Extended JsonToTomlTests with array conversion
257. Added project_doc_max_bytes config field with CLI overrides
258. ProjectDoc.GetUserInstructions now accepts size and path overrides
259. Exec and interactive commands expose --project-doc-max-bytes and --project-doc-path
260. Added messages-watch subcommand to history command
261. Implemented MessageHistory.WatchEntriesAsync API
262. Added unit tests for project doc limit and message watching
263. Introduced McpServer handling JSON-RPC over HTTP with initialize, ping, tools/list and tools/call
264. McpCommand now runs McpServer instance
265. Added PingAsync to McpClient and --ping option to mcp-client command
266. Created McpServerTests verifying initialize and list tools
267. Added TestUtils helper for free TCP port lookup
268. Added minimal event broadcasting API in McpServer
269. Added stub handlers for resources, prompts, subscribe and logging requests in McpServer
270. Extended McpClient with methods for new MCP requests
271. Added record types for resources, prompts and completion results
272. Added prompts/list test to McpServerTests
273. Added CodexToolRunner placeholder with event emission
274. McpServer now handles tools/call via CodexToolRunner
275. Added CreateResponse helper and refactored request dispatch
276. Implemented CallCodexAsync helper in McpClient
277. Added codex tool-call test in McpServerTests
278. Added CodexToolCallParam record for codex configuration
279. McpServer returns structured responses for various requests
280. Expanded AGENTS.md with new features and todo list
281. Implemented stub Codex events streaming to /events clients
282. Updated tests and build
283. Added roots/list handler in McpServer with default root
284. Implemented simple in-memory resource store and resources/list & resources/read handlers
285. Tools/list now returns basic schema for codex tool
286. SSE events are now JSON serialized for clients
287. Added Root and ListRootsResult records and ListRootsAsync in McpClient
288. McpClientCommand gained --list-roots and --read-resource options
289. Added unit tests covering roots/list and resource APIs
290. Updated McpServerTests to verify new endpoints
291. Documented new progress and updated TODO list
292. MCP server now emits demo resource for testing
293. Added prompt storage with sample prompt
294. Prompts/list and prompts/get now return stored data
295. Resource templates listing implemented with demo template
296. Added resources/write endpoint with disk persistence
297. Implemented logging/setLevel handler storing value
298. Added completion/complete handler returning stub completion
299. McpClient exposes WriteResourceAsync and WriteResourceRequestParams
300. McpClientCommand supports prompts, templates, resource writing, log level and completion options
301. Updated McpServerTests with coverage for new features
302. Documented progress and updated TODO list
304. Added resource list change and update events
305. Implemented subscribe/unsubscribe tracking
306. Initialization response now includes protocol version and capabilities
307. McpClient exposes SubscribeAsync and UnsubscribeAsync
308. McpClientCommand supports subscribe and unsubscribe options
309. McpServerTests verify initialization result fields
310. Added subscription test using SSE events

## TODO Next Run
- Continue porting remaining Rust CLI features
- Investigate hanging tests and fix missing API key issues
- Flesh out CodexToolRunner with real Codex integration
