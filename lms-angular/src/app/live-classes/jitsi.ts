// Community Jitsi instance used for live classes. Unlike the public meet.jit.si it
// carries no 8x8 promo — but community servers block iframe embedding, so we open
// the room in a new browser tab rather than embedding it. Change this one line to
// use a different community instance or your own self-hosted Jitsi.
export const JITSI_DOMAIN = 'meet.ffmuc.net';

/** Full room URL on the community server, opened in a new tab. */
export function jitsiRoomUrl(roomName: string): string {
  return `https://${JITSI_DOMAIN}/${encodeURIComponent(roomName)}`;
}
