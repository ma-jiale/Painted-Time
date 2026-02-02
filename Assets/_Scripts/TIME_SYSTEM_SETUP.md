# Time Manipulation System - Setup Guide

## 📋 Overview
This is a complete, modular time manipulation system for Unity VR. It allows players to control a "TimeValue" (-1 to 1) on objects, causing different effects like growth, movement, or puzzle unlocking.

**Key Concept**: This is NOT time reversal or physics rewind. It's a **value-driven system** where objects respond to a simple number.

---

## 🎯 Core Scripts

### 1. **TimeAnchor.cs**
- **Purpose**: Stores the TimeValue (-1 to 1) for any time-manipulable object
- **Features**:
  - Safe value modification with clamping
  - Boss interference simulation (pulls time in a direction)
  - Events for external listeners
- **Attach to**: Any GameObject that needs to be affected by time

### 2. **TimableObject.cs** (Abstract Base Class)
- **Purpose**: Base class for all objects that respond to time changes
- **Features**:
  - Automatic TimeAnchor management
  - Visual feedback during manipulation
  - Helper methods for time value mapping
- **Attach to**: Never directly - use derived classes

### 3. **TimeInteractionController.cs**
- **Purpose**: Handles VR player input to manipulate time
- **Features**:
  - Raycast detection of Timable objects
  - Trigger button to enter/exit time mode
  - Left stick X-axis to drag timeline
  - Visual ray indicator
  - Timeline UI management
- **Attach to**: VR Rig or XR Origin

### 4. **TimeStabilityChecker.cs**
- **Purpose**: Creates the "time stabilization challenge" for cage puzzles
- **Features**:
  - Configurable stability range (e.g., 0.4 to 0.6)
  - Required duration timer
  - UnityEvents for unlock triggers
  - Optional partial progress mode
- **Attach to**: Cage objects that need stability challenges

---

## 🌲 Example Implementations

### **TimableTree.cs**
- **Effect**: Tree grows/shrinks based on time
  - -1 = Small/young
  - 0 = Normal
  - +1 = Large/old
- **Attach to**: Tree models

### **TimablePlatform.cs**
- **Effect**: Platform moves along waypoints
  - -1 = Start position
  - 0 = Middle position
  - +1 = End position
- **Attach to**: Moving platform objects

### **TimableCage.cs**
- **Effect**: Complete cage puzzle with stability checker
- **Features**: Door animation, visual feedback, unlock events
- **Attach to**: Cage objects with TimeStabilityChecker

### **TimelineUIController.cs**
- **Effect**: Displays timeline UI with slider and stability progress
- **Attach to**: Canvas GameObject for timeline interface

---

## 🚀 Quick Setup Guide

### Step 1: Setup VR Controller
1. Create or locate your **VR Rig** (XR Origin)
2. Add `TimeInteractionController.cs` to the rig
3. Configure the component:
   - **Left Stick Action**: Assign your left controller stick input action
   - **Right Trigger Action**: Assign your right trigger input action
   - **Ray Origin**: Assign the camera or right controller transform
   - **Timable Tag**: Set to "Timable" (or create your own tag)
   - **Timeline UI**: Assign your UI canvas (create in Step 3)

4. Optional: Add a `LineRenderer` component to visualize the aim ray
   - Assign it to the "Aim Ray" field in TimeInteractionController

### Step 2: Create a Timable Tree
1. Create a **3D Cube** (or use a tree model)
2. Add **TimableTree.cs** component
3. The TimeAnchor will be added automatically
4. Configure:
   - **Min Scale**: 0.3 (young tree)
   - **Normal Scale**: 1.0
   - **Max Scale**: 2.0 (old tree)
5. **Important**: Add tag "Timable" to the GameObject
   - Edit → Project Settings → Tags and Layers → Add "Timable"
   - Select GameObject → Tag → Timable

### Step 3: Create Timeline UI (Optional but Recommended)
1. Create a **Canvas** (World Space recommended for VR)
2. Add `TimelineUIController.cs` to the Canvas
3. Create UI elements as children:
   - **Slider**: Shows time value (-1 to 1)
   - **Text (TMP)**: Shows current time value
   - **Text (TMP)**: Shows time state (Past/Present/Future)
   - **Image**: Progress bar for stability
4. Assign these elements to TimelineUIController fields
5. Assign this Canvas to TimeInteractionController's "Timeline UI" field

### Step 4: Create a Moving Platform
1. Create a **3D Cube** (platform)
2. Add **TimablePlatform.cs** component
3. Choose movement mode:
   - **Simple Movement**: Set "Use Simple Movement" = true
     - Configure Start Position and End Position
   - **Waypoint Movement**: Set "Use Simple Movement" = false
     - Create empty GameObjects as waypoints
     - Assign them to "Path Waypoints" array
4. Add tag "Timable" to the GameObject

### Step 5: Create a Treasure Cage Puzzle
1. Create a **Cage** model (or use a cube as placeholder)
2. Add **TimableCage.cs** component
3. Configure TimeStabilityChecker settings:
   - **Stability Range Min**: 0.4
   - **Stability Range Max**: 0.6
   - **Required Stability Duration**: 3.0 seconds
4. Create a **treasure** GameObject inside the cage
5. Disable the treasure initially
6. Assign treasure to "Unlocked Object" field
7. Configure UnityEvents:
   - **OnUnlocked**: Enable treasure, play effects, etc.
8. Add tag "Timable" to the cage GameObject

### Step 6: Add Boss Interference (Optional)
1. Select any Timable object
2. Find the **TimeAnchor** component
3. Configure Boss Interference:
   - **Allow Boss Interference**: true
   - **Boss Interference Strength**: 0.3
   - **Boss Interference Direction**: -1 (pulls toward past)
4. In your boss script, call:
   ```csharp
   timeAnchor.SetBossInterference(true);
   ```

---

## 🎮 Input Configuration

### For Unity Input System (Recommended)
1. Open your **Input Actions** asset (e.g., `InputSystem_Actions.inputactions`)
2. Ensure you have these actions:
   - **XRI LeftHand/Position** (Vector2) - for stick input
   - **XRI RightHand/Activate** (Float) - for trigger
3. In TimeInteractionController:
   - Assign "XRI LeftHand/Position" to Left Stick Action
   - Assign "XRI RightHand/Activate" to Right Trigger Action

### For Legacy XR Input
If you're using legacy input, modify TimeInteractionController.cs:
```csharp
// Replace Input Action reads with:
Vector2 stickInput = new Vector2(
    Input.GetAxis("XRI_Left_Primary2DAxis_Horizontal"),
    Input.GetAxis("XRI_Left_Primary2DAxis_Vertical")
);

float triggerValue = Input.GetAxis("XRI_Right_Trigger");
```

---

## 🧪 Testing Your Setup

### Quick Test Scene
1. Create a new scene
2. Add VR Rig with TimeInteractionController
3. Add 2-3 TimableTrees with "Timable" tag
4. Add 1 TimablePlatform with "Timable" tag
5. Enter Play Mode
6. Point at a tree
7. Press right trigger
8. Move left stick left/right
9. Watch the tree grow/shrink and platform move!

### Debugging Tips
- Enable Gizmos in Scene view to see:
  - TimeAnchor stability indicators
  - Platform paths
  - Cage stability ranges
- Check Console for debug logs:
  - "Entered time manipulation on: [object]"
  - "Exited time manipulation"
  - Stability state changes

---

## 🎨 Customization

### Create Your Own Timable Object
```csharp
using UnityEngine;

public class TimableCustomObject : TimableObject
{
    protected override void ApplyTimeValue(float timeValue)
    {
        // Your custom behavior here
        // timeValue ranges from -1 (past) to +1 (future)
        
        // Example: Change color
        float hue = (timeValue + 1f) / 2f; // Map to 0..1
        GetComponent<Renderer>().material.color = Color.HSVToRGB(hue, 1, 1);
    }
}
```

### Extend Boss Interference
In your boss AI script:
```csharp
public class BossAI : MonoBehaviour
{
    [SerializeField] private TimeAnchor targetCage;
    
    void StartInterference()
    {
        // Pull time toward past
        targetCage.SetBossInterference(true);
        targetCage.ConfigureBossInterference(0.5f, -1f);
    }
    
    void StopInterference()
    {
        targetCage.SetBossInterference(false);
    }
    
    void TimePulseAttack()
    {
        // Sudden jolt
        targetCage.ApplyTimeJolt(0.3f);
    }
}
```

---

## 📊 Performance Notes

- **No Update loops for inactive objects**: TimableObjects only process when their TimeValue changes
- **Smooth transitions**: All visual changes use Lerp for smooth 60fps performance
- **Minimal raycasts**: Only one raycast per frame in TimeInteractionController
- **Event-driven**: Uses C# events instead of polling

---

## 🐛 Common Issues

### Issue: "Timable objects not detected"
- **Solution**: Ensure GameObject has "Timable" tag
- Check raycast layer mask in TimeInteractionController
- Verify object has a Collider component

### Issue: "Timeline UI not showing"
- **Solution**: Check Timeline UI is assigned in TimeInteractionController
- Ensure Canvas is in World Space mode for VR
- Check UI GameObject is initially inactive

### Issue: "Cage not unlocking"
- **Solution**: Check TimeStabilityChecker range (0.4 to 0.6)
- Ensure player is holding TimeValue stable
- Check required duration (default 3 seconds)
- Enable "Allow Partial Progress" for easier difficulty

### Issue: "Boss interference not working"
- **Solution**: Call `SetBossInterference(true)` from your boss script
- Verify "Allow Boss Interference" is checked in TimeAnchor
- Increase "Boss Interference Strength" for stronger effect

---

## 📚 Architecture Summary

```
Player → TimeInteractionController
              ↓ (raycast + input)
         TimableObject
              ↓ (requests change)
         TimeAnchor
              ↓ (fires event)
         TimableObject.ApplyTimeValue()
              ↓ (visual change)
         Tree grows / Platform moves / Cage checks stability
```

---

## ✅ Checklist for Student Projects

- [ ] VR Rig has TimeInteractionController
- [ ] Input actions are assigned (trigger + stick)
- [ ] At least one test object with "Timable" tag
- [ ] Timeline UI canvas created and assigned
- [ ] One TimableTree for testing
- [ ] One TimablePlatform for testing
- [ ] One TimableCage with TimeStabilityChecker
- [ ] Boss interference tested (if needed)
- [ ] Scene tested in VR headset

---

## 🎓 Educational Notes

This system is designed for **student projects** with these priorities:

1. **Clarity over complexity**: Simple value-driven system, not true time reversal
2. **Modularity**: Easy to extend with new TimableObject types
3. **Visual feedback**: Clear indicators of time state
4. **Iterative design**: Add features incrementally
5. **No hidden magic**: All behavior is explicit and commented

---

## 📝 Next Steps

1. **Test basic interactions**: Tree growth and platform movement
2. **Build first cage puzzle**: 3-second stability challenge
3. **Add boss interference**: Create tug-of-war gameplay
4. **Design multiple puzzle types**: Combine trees, platforms, and cages
5. **Polish visuals**: Add particle effects and audio feedback
6. **Playtest with peers**: Adjust difficulty based on feedback

---

**System Version**: 1.0  
**Unity Version**: 6.3  
**VR Framework**: XR Interaction Toolkit  
**Last Updated**: January 2026

For questions or issues, check the inline code comments - every script has detailed explanations!
