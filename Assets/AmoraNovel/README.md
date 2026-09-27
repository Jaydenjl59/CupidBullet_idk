# Cupid Bullet — playable chapter

Open the existing Unity project and press Play. The editor is configured to start the new Amora scene. Its project location is Assets/AmoraNovel/Amora.unity.

## Included

- Title screen with the supplied Start artwork and Amora portrait.
- Original compiled Ink story, player name entry, typewriter dialogue, expressions, character fades, pink transitions, dialogue history, choices, music, pause, and one save slot.
- Courtyard background using the supplied cherry-blossom artwork; a simple café interior made from UI shapes; supplied festival artwork on the title screen.
- Two skill checks with different patterns. Survive for 25 / 32 seconds or shoot down the heart barrier. Three hits lose the attempt. A brief invulnerability period follows each hit.
- Real outcomes automatically choose Ink's Win / Lose branches and record the result in the existing GameData class.
- A chapter-end screen for the date / friendship outcomes.

## Controls

Click the dialogue box, Space, or Enter: reveal / advance dialogue. Click a response to choose it.
WASD or arrow keys: move during battles. Shift: precision movement. Hold Space: shoot. Escape: pause.
Save is available during dialogue. Continue on the title screen resumes the save.

## Story boundaries and preserved behavior

The supplied story ends at AUTUMN FESTIVAL UNLOCKED. No festival chapter was present, so the game ends there rather than adding new story text.

The supplied Ink retains the second encounter's failure counter through its rejection/time-loop route. Winning after two or more failures still returns to that route. This authored behavior is preserved. Start a new story from the title screen to reset it. Changing that progression requires a story-design decision; the original Ink was not edited.

The existing C# scripts and compiled Ink file are unchanged. New code is under Assets/AmoraNovel. The existing battle prototype remains available in its original scenes; the visual novel uses a separate self-contained battle controller and the existing GameData result API.

## Validation

Twelve story traversals covered three dialogue-choice variants across first-try success, retry success, friendship, and rejection loops. Runtime integration checks covered name entry, event-triggered battles, pause, movement bounds, focus speed, isolated shooting victory, collision loss, retry, survival victory, dialogue return, save/load, the second skill check, and the date ending. Screens were also inspected in Unity Play mode.

The editor's Amora menu includes story validation. A standalone Windows build is in the Unity project's Builds/Amora folder when build validation succeeds.

The existing SampleScene had unsaved changes when work began. Those were saved for the standalone build; a copy of its previous on-disk version is included in the handoff backups.

