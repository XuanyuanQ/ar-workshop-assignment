How I Built The App
- I built an Android AR app in Unity using AR Foundation and ARCore.
- I followed the surface-based workshop to set up plane detection and tap-to-place interaction.
- I followed the marker-based workshop to set up image tracking and marker content spawning.
- I built two scenes to implement the two parts separately.
Surface-Based Part
- The app detects real-world flat surfaces.
- The user can tap a detected surface to place a virtual object.
- I modified the original tap-to-place code so the object stays smaller than the detected plane.
- I also added simple selection and dragging for placed objects.
- I also added a small status display in the top-left corner to show the AR session state, detected planes, tracked images, and placed objects.
Marker-Based Part
- The app detects a printed marker image.
- When the marker is found, a dice model appears on it.
- I modified the workshop spinning-cube code so the dice spins from swipe input.
- The dice slows down after the swipe and stops in a readable orientation.
Code Sources
- Surface placement was adapted from the surface-based workshop.
- Marker tracking and the first spin script were adapted from the marker-based workshop.
- My own changes included plane-relative object sizing, object dragging, the dice model, swipe-based rotation, final dice settling behavior, and an AR status display.
Testing
- I built the app for Android and tested it directly on an Android phone.
- I tested plane detection, object placement, marker detection, and dice spinning.
code Link
https://github.com/XuanyuanQ/ar-workshop-assignment
