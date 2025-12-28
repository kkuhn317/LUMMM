import os
import csv

root = os.path.join(os.getcwd(), 'Assets', 'Scripts')

# Heuristic mapping rules
exception_map = {
    'Utils/PlayAudioAfterDelay.cs': 'Systems/Audio/PlayAudioAfterDelay.cs',
    'Utils/MusicChangeArea.cs': 'Systems/Audio/MusicChangeArea.cs',
    'Utils/InputActionUIHandler.cs': 'UI/Input/InputActionUIHandler.cs',
    'Utils/GlobalInputLock.cs': 'Input/Global/GlobalInputLock.cs',
    'Utils/GlobalInputBlockerUI.cs': 'UI/Components/GlobalInputBlockerUI.cs',
}

ui_screens_keywords = ['Menu', 'Loader', 'LevelInfo', 'Intro', 'GameOver', 'Unpause', 'Credits']
ui_controls_keywords = ['Button', 'Control', 'DPad', 'Swipe', 'Activator', 'KeyPress', 'Mobile', 'Draggable']

mappings = []

for dirpath, dirnames, filenames in os.walk(root):
    for f in filenames:
        if not f.endswith('.cs'):
            continue
        full = os.path.join(dirpath, f)
        rel = os.path.relpath(full, root).replace('\\', '/')
        src = os.path.join('Assets', 'Scripts', rel).replace('\\', '/')

        # Apply exceptions first
        matched = False
        for k, v in exception_map.items():
            if rel.startswith(k):
                dst = os.path.join('Assets', 'Scripts', v).replace('\\', '/')
                matched = True
                break

        if not matched:
            # Generic replacements
            dst_rel = rel
            dst_rel = dst_rel.replace('Abstracts/', 'Core/Abstracts/')
            dst_rel = dst_rel.replace('Audio/', 'Systems/Audio/')
            dst_rel = dst_rel.replace('Camera/', 'Systems/Camera/')
            dst_rel = dst_rel.replace('Characters/Movement Overrides/', 'Characters/MovementOverrides/')
            dst_rel = dst_rel.replace('Characters/Abilities/', 'Characters/Abilities/')
            dst_rel = dst_rel.replace('Effect Areas/', 'Systems/EffectAreas/')
            dst_rel = dst_rel.replace('Interfaces/', 'Core/Interfaces/')
            dst_rel = dst_rel.replace('Level Specific/', 'Levels/')
            dst_rel = dst_rel.replace('Rebind Action/', 'Input/Rebinding/')
            dst_rel = dst_rel.replace('Scriptable Objects/', 'ScriptableObjects/')
            dst_rel = dst_rel.replace('Fonts/', 'UI/Fonts/')
            dst_rel = dst_rel.replace('Utils/', 'Core/Utils/')
            dst_rel = dst_rel.replace('Effect Areas/', 'Systems/EffectAreas/')

            # UI heuristics
            if rel.startswith('UI/'):
                name = f
                dst_sub = 'UI/Components/'
                for kw in ui_screens_keywords:
                    if kw in name:
                        dst_sub = 'UI/Screens/'
                        break
                else:
                    for kw in ui_controls_keywords:
                        if kw in name:
                            dst_sub = 'UI/Controls/'
                            break
                dst_rel = dst_rel.replace('UI/', dst_sub)

            # Objects keep structure under Objects/
            # Settings, Managers, Data, Demo, Tools, ScriptableObjects remain in place

            dst = os.path.join('Assets', 'Scripts', dst_rel).replace('\\', '/')

        mappings.append((src, dst))

# Write CSV
out = os.path.join(os.getcwd(), 'Assets', 'Scripts', 'reorg_map.csv')
with open(out, 'w', newline='', encoding='utf-8') as csvfile:
    writer = csv.writer(csvfile)
    writer.writerow(['current_path', 'proposed_path'])
    for s, d in sorted(mappings):
        writer.writerow([s, d])

print(f'Wrote {out} with {len(mappings)} entries')
