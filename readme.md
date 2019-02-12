# Clipboard++

reason: I wanted an application that I could use that kept an in memory cache of mutiple clipboard items. That way if I accidently copied over some data I was working on, I could go back to the original version

## Notes / Considerations
* Encryption will be done only on the saving of the total item at time of exit. Considered the idea of using in Memory encryption, and I think it could benifit strings, however, it probibly is not needed at this time, as any "clipboard viewer" could sit back and sniff the data easier then reading my app
* I put hard coded limits on the file size for saving... I dont want to waste a bunch of time, writing a 2+ gig file to drive when you probibly wont ever need it. Besides, there is now the save functionality built in
* removed the project of using hot keys or locking items. A collection would have to rebuild itself in memory every time, or I would have to manage 2 different collection types, which gets difficult with large data sets
* Since this is open source, if anyone would like to fork this project, I am more then willing to have others help

## future enhancements
* ~~Be able to save clipped items~~
* ~~store / retrieve clip items on drive for reboots~~
  * ~~cryptography on stored items
* ~~In memory compression on text items~~
* ~~potential compression on images... need to research what is actually saved on clip~~
* ~~options form, to configure compression, max number of items, and cryptography
* ~~pre defined hotkeys for cached items
* ~~Lock particular cache items to list
* ~~prevent duplicate items from being in memory~~ - Note: would still like to research format17/dib/Bitmap compression more
* ~~better about me screen
* winform with better display of cache items (optional)
  * Drag and drop support
  * Save support
* ~~have a better icon then "default"~~
* ~~experiment with 3rd party serialization engines for better performance and or compression~~
* ~~pause / resume button~~

## Learnings

### New
* low level pointers
* dll import
* In Memory image formating
* Message Pack  (https://msgpack.org) serialization and deserialization


### Existing / reinforce
* sha1 hashing
* binary serialization
* winform development
* notification icon development
* System Cryptograghy Namespace

