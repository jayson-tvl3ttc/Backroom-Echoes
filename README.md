#  Backroom Echoes  
**Spatial Audio-Driven Navigation and Threat Detection in VR Horror**  

---

##  Project Download

Due to GitHub’s file size limit, the full Unity project (including audio assets and VR build)  
is hosted on Google Drive:

**[Download Backroom Echoes (Google Drive)](https://drive.google.com/drive/folders/1kk2eFBku6gEOXTBAHxCbep0okqpAZjdM?usp=drive_link)**

This repository contains only documentation.  
For full project access, please download via the link above.


---

##  Overview

**Backroom Echoes** is a **VR horror survival experience** that explores how **3D spatial audio** can serve as the *primary* interface for **navigation** and **threat detection** in immersive virtual environments.  
The project places players in a labyrinthine "Backrooms"-inspired maze, where sound is the key to survival.

Players must activate multiple generators scattered throughout the maze while evading a patrolling enemy.  
Every gameplay mechanic—from navigation to AI behavior—is controlled by **audio cues**, demonstrating the potential of sound as a functional, emotional, and spatial navigation tool in VR.

Developed in **Unity (URP + Steam Audio)** with full **VR support (XR Interaction Toolkit)**, this project investigates the intersection of sound design, player psychology, and technical implementation in extended reality environments.

---

##  Core Concept

> “When vision becomes unreliable, hearing becomes survival.”

Backroom Echoes challenges traditional visual-first game design by making **sound perception the main sensory channel** for interaction.  
Instead of maps or HUDs, players rely on **directional sound cues**—like generator hums, enemy footsteps, and heartbeat feedback—to locate objectives and avoid threats.

Spatial audio not only enhances immersion but *redefines gameplay logic*, shaping how players make decisions under pressure in a sound-dominated world.

---

##  Environment and Aesthetics

- The environment draws from the **“Backrooms” aesthetic**: endless yellow corridors, fluorescent lighting, and unsettling liminal spaces.  
- Assets were sourced from **Sketchfab** and optimized for acoustic realism, ensuring proper reflection, occlusion, and reverberation behavior.  
- The maze’s architecture was designed with **sound propagation** in mind, creating realistic echo patterns that help players locate generators or threats using auditory clues.  
- Environmental sounds—such as flickering lights, dripping water, and mechanical noises—reinforce the sense of unease while providing spatial reference points.

### Game Screenshots
<img width="1428" height="805" alt="屏幕截图 2025-05-20 230655" src="https://github.com/user-attachments/assets/156980fc-0d2e-4cc1-b497-23298f45ac67" />

<img width="1418" height="805" alt="屏幕截图 2025-05-20 230507" src="https://github.com/user-attachments/assets/6774c986-890f-4526-9a1a-326f4bfb5a06" />

<img width="1427" height="803" alt="屏幕截图 2025-05-20 230856" src="https://github.com/user-attachments/assets/5c46bc6e-6d94-413d-a0d8-71e06657673f" />

<img width="1425" height="802" alt="屏幕截图 2025-05-20 231046" src="https://github.com/user-attachments/assets/c38efc7c-388c-4bd1-b2ad-53915e361c56" />

<img width="1436" height="809" alt="屏幕截图 2025-05-20 230336" src="https://github.com/user-attachments/assets/17f64b8d-c3ac-428e-922b-99fd4f997324" />

---
##  Gameplay Design

### Player Objectives
- Activate **four generators** hidden throughout the maze.  
- Avoid detection by the enemy entity using **sound-based stealth**.  
- Escape via the **elevator**, which activates only after all generators are powered.

### Core Loop
1. Listen for **generator malfunction sounds** to determine their direction.  
2. Approach and interact with the generator for ~16 seconds (continuous activation).  
3. Once activated, a loud startup sound attracts the **enemy**, forcing players to hide or flee.  
4. Repeat until all generators are active, then navigate by **elevator audio cue** to escape.


---

##  Sound Interaction System

### Spatial Audio Framework
- **Steam Audio** middleware used for real-time 3D audio propagation and reflection.  
- **HRTF rendering** ensures accurate sound localization in full 360° space.  
- Each sound source includes **distance attenuation**, **occlusion**, and **reverb** parameters tuned for VR.  
- Materials in the maze are acoustically modeled (concrete, carpet, metal) to create realistic environmental acoustics.

### Key Audio Mechanics

| Interaction | Audio Design | Purpose |
|--------------|---------------|----------|
| **Generator** | Electrical fault noise → activation → running hum | Serves as navigation beacon & AI trigger |
| **Enemy** | Footsteps, breathing, reactive sound to player proximity | Main threat localization and tension feedback |
| **Environment** | Flickering lights, ambient hum, spatial reverb | Builds atmosphere and spatial orientation |
| **Player Feedback** | Heartbeat intensity based on distance to enemy | Indicates danger without visual cues |

All audio assets were processed using **Freesound.org** resources with additional dynamic filters applied for realism.

---

##  Enemy AI and Sound Integration

The enemy AI operates through a **five-state finite state machine**:  
`Patrol → Wait → Investigate → Search → Chase`.

- **Audio-Triggered Behavior:** When a generator activates, its startup sound triggers the *InvestigateSound()* event in the AI system, redirecting the enemy toward that sound’s origin.  
- **Dynamic Audio Feedback:** Enemy footstep tempo changes by state (fast during chase, slow during patrol).  
- **Bidirectional Interaction:** Player-generated sounds influence AI decisions, while AI sounds inform the player’s survival strategy.  
- **Dual Detection System:** Combines visual (raycast-based) and auditory detection for realistic enemy awareness.

This creates a **feedback loop** between sound emission and gameplay consequence—where every action heard by the enemy alters the player’s next move.

---

##  Experimental Evaluation

Data was collected every 30 frames during gameplay for research analysis.

### Key Metrics
- **Player-Enemy Distance:** Players maintained safe distances 70% of the total time.  
- **Threat Detection Rate:** 85% success using *audio-only* information.  
- **Reaction Time:** Average of 2–3 data frames between detection and avoidance.  
- **Head Rotation Patterns:** Spikes corresponded with sound localization behaviors, confirming that players actively used head movement to identify audio sources.

### Findings
- Players developed **audio-driven navigation strategies** comparable to visual navigation systems.  
- The layered sound design effectively balanced **immersion and playability**.  
- Spatial audio cues promoted **natural scanning behaviors** and situational awareness in VR.

---

##  Privacy and Ethics

All user data was collected with explicit consent under **privacy-by-design** principles:
- No personal identifiers or biometric data stored.  
- Session-based anonymized IDs (UUID system).  
- Local data storage only; no cloud uploads.  
- Full compliance with GDPR and academic ethics guidelines.

---

##  Technologies Used

| Category | Tool / Framework |
|-----------|------------------|
| Game Engine | Unity (URP + XR Interaction Toolkit) |
| Audio Middleware | Steam Audio |
| Audio Sources | Freesound.org |
| Programming | C# |
| Data Analysis | MATLAB |
| Hardware | HTC Vive / Oculus Quest 2 |
| 3D Assets | Sketchfab (Backrooms Environment) |

---

##  Demo Video

 [Watch the Backroom Echoes Demo]()  

---

##  Future Work

- Expand to multi-enemy systems for complex spatial awareness scenarios.  
- Integrate **audio-based puzzle mechanics** and **interactive story elements**.  
- Develop **difficulty adjustment systems** for player accessibility.  
- Introduce **custom HRTF profiles** for personalized localization performance.

---

##  Author

**Jayson Chen**  
Master’s in Audio and Music Technology  
University of York  



