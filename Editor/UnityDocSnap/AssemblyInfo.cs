// ==========================================
// AssemblyInfo
// Exposes this editor assembly's internal types
// to the EditMode test assembly only. Everything
// in Unity DocSnap is intentionally `internal`
// (it is a self-contained editor tool, not a
// public API surface), so the tests would have no
// way to reach UniversalReflector / JsonValue /
// DocSnapSummaryWriter without this grant. The
// test assembly is Editor-only and gated behind
// UNITY_INCLUDE_TESTS, so this never affects a
// normal build.
// ==========================================
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AmirCollider.UnityDocSnap.Editor.Tests")]
