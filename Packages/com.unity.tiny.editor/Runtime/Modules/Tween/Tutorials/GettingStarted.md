# TweenService

The `{@link ut.tween.TweenService}` allows you to create tweens and specify default behaviour.

A singleton instance `{@link Tween}` exists that is already setup on the default world. 

Use `Tween.create()` to build a new tweener object

Use `Tween.sequence()` to build a new sequence object

A tween, tweener or sequence can be decorated with **components** using a builder pattern.

# Tweens

A `{@link ut.tween.Tween}` represents the abstract base class for a tween. 

`.setDelay(delay)`
Adds a delay to the start of the tween.

`.setDuration(duration)` 
Sets the duration of the tween; default is 1 second.

`.setLoop(loopType, loopCount = -1)`
Sets the looping options { Restart, PingPong } for the tween. Setting **loopCount** to -1 will make the tween loop infinitely. 

`.isDone()`
Returns true if the tween has completed

`.play()`
Plays the tween.

`.stop()`
Stops the tween.

# Tweeners

A `{@link ut.tween.Tweener}` handles interpolating a specific value of a component or struct

`.setTarget(entity, component, property)` Sets the target of the tween; the value to tween.

**Example** 
```javascript
// tweens the x position from 0 to 100
Tween.create()
	 .setTarget(entity, ut.Core2D.Transform, 'localPosition.x')
	 .setFloat(0, 100);
```

`.setFloat(start, end)` Tweens a float value from start to end.

`.setVector2(start, end)` Tweens a Vector2 value from start to end.

`.setColorRGBA(start, end)` Tweens a ColorRGBA value from start to end.

**IMPORTANT** The target and tween value must match and has no error handling~! (i.e. Don't set the `setTarget` to Vector2 and use `setFloat` to tween the value)

{@link ut.tween.Tweener|Full Tweener API}

# Sequences

A `{@link ut.tween.Sequence}` handles chaining or parallelizing tweens

`.then(tween)` Chains the tween with the sequence.

`.and(tween)` Parallelizes the tween with the sequence

# Component Extensions

Component extension methods exist to quickly play tweens on object. These methods can be decorated as normal.

**Example**
```javascript
// tweens the localPosition of the transform
transform.tween('localPosition', new Vector2(0,0,0), new Vector2(100,100,0));

// tweens the alpha of the spriteRenderer from 0 to 255
spriteRenderer.tween('color.a', 0, 255);
```

**IMPORTANT** The target and tween value must match and has no error handling~! (i.e. Don't try `transform.tween('localPosition', 0, 255);` )

# Examples

```javascript
spriteRenderer.tween('color.a', 0, 255)
			  .setEase(ut.tween.EaseType.EaseInOutQuad)
			  .setDuration(0.2)
			  .setLoop(ut.tween.LoopType.PingPong);
```