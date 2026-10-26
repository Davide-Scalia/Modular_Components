# 2D car setup

1. Assign `Assets/Settings/InputSystem_Actions.inputactions` to `CarPlayerInput2D`. Its `Driving` map is included: A/D or left/right arrows steer, W/S or up/down arrows accelerate and reverse, and the gamepad left stick plus triggers provide analog controls.
2. Make a wrapper object named `CarActor`. Add `CarPlayerInput2D` to it.
3. Add a child named `PhysicsBody` with a Dynamic `Rigidbody2D`, a non-trigger `Collider2D`, and `CarController2D`. Set gravity scale to zero, interpolation to Interpolate, and collision detection to Continuous. Assign this controller as the input reader's receiver.
4. Add a sibling child named `VisualRoot` with the sprite, animator, or other art. Add `CarVisualFollower2D` there and assign the `PhysicsBody` rigidbody. Set the art's rotation offset if its forward direction is not local up.
5. Put obstacles on collidable layers with non-trigger `Collider2D` components. The controller uses collision contacts for bounce response and blocks engine force into active contacts.

The wrapper stays unrotated. Keep `PhysicsBody` and `VisualRoot` as siblings; the visual follower copies the physics body's world pose rather than inheriting its transform.

Tune acceleration, reverse speed, steering, lateral grip, bounce restitution, and impact speed retention on `CarController2D` in the Inspector. The physics body should use colliding (non-trigger) colliders; continuous collision detection is recommended for fast movement.
