# Clipboard++

reason: I wanted an application that I could use that kept an in memory cache of mutiple clipboard items. That way if I accidently copied over some data I was working on, I could go back to the original version

## future enhancements
* Be able to save clipped items
* store / retrieve clip items on drive for reboots
  * cryptography on stored items
* ~~In memory compression on text items~~
* potential compression on images... need to research what is actually saved on clip
* options form, to configure compression, max number of items, and cryptography
* pre defined hotkeys for cached items
* Lock particular cache items to list
* prevent duplicate items from being in memory
* better about me screen
* winform with better display of cache items (optional)
  * Drag and drop support
  * Save support
* have a better icon then "default"
* experiment with 3rd party serialization engines for better performance and or compression
* pause / resume button

## Learnings

### New
* low level pointers
* dll import

### Existing / reinforce
* sha1 hashing
* binary serialization
* winform development
* notification icon development
