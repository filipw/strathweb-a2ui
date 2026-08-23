// The client half of the sample. It knows the A2A protocol and A2UI, and nothing about coffee.
//
// The renderer is the official @a2ui/lit package, loaded straight from esm.sh so the sample needs no
// npm install or bundler. A real application would install it and bundle it.
import { MessageProcessor } from 'https://esm.sh/@a2ui/web_core@0.10.6/v0_9';
import { A2uiSurface, basicCatalog, Context } from 'https://esm.sh/@a2ui/lit@0.10.3/v0_9';
import { renderMarkdown } from 'https://esm.sh/@a2ui/markdown-it@0.1.1';

const A2UI_MIME = 'application/a2ui+json';
const CONTEXT_ID = crypto.randomUUID();

const surfaces = document.getElementById('surfaces');
const entries = document.getElementById('entries');

// The Text component renders Markdown and asks its ancestors for a renderer through the Lit context
// protocol. Answering it here is what turns a heading variant into a heading rather than "## text".
surfaces.addEventListener('context-request', event => {
  if (event.context === Context.markdown) {
    event.stopPropagation();
    event.callback(renderMarkdown);
  }
});

// Every action the user takes on any surface comes through here.
const processor = new MessageProcessor([basicCatalog], action => send({ action: toWire(action) }), {
  version: 'v0.9.1',
});

processor.onSurfaceCreated(surface => {
  surfaces.querySelector('.placeholder')?.remove();

  const element = new A2uiSurface();
  element.surface = surface;
  element.id = `surface-${surface.id}`;
  surfaces.append(element);
});

processor.model.onSurfaceDeleted.subscribe(id => {
  document.getElementById(`surface-${id}`)?.remove();
});

/** Sends one message to the agent and applies whatever A2UI comes back. */
async function send(part) {
  // The pinned A2A SDK uses the proto JSON mapping: parts carry no "kind" discriminator, and
  // enum values are spelled ROLE_USER rather than "user".
  const parts =
    'action' in part
      ? [{ data: [{ version: 'v0.9.1', action: part.action }], metadata: { mimeType: A2UI_MIME } }]
      : [{ text: part.text }];

  log('out', 'action' in part ? `action ${part.action.name}` : `text "${part.text}"`);

  const body = {
    jsonrpc: '2.0',
    id: crypto.randomUUID(),
    method: 'SendMessage',
    params: {
      message: {
        role: 'ROLE_USER',
        messageId: crypto.randomUUID(),
        contextId: CONTEXT_ID,
        parts,

        // How the agent learns what this client can render, and what the user has typed so far.
        metadata: {
          a2uiClientCapabilities: processor.getClientCapabilities(),
          ...dataModelMetadata(),
        },
      },
    },
  };

  let payload;
  try {
    const response = await fetch('/a2a', {
      method: 'POST',
      headers: {
        'content-type': 'application/json',
        // Asking the agent to activate the A2UI extension. Optional, but polite.
        'X-A2A-Extensions': 'https://a2ui.org/a2a-extension/a2ui/v0.9.1',
      },
      body: JSON.stringify(body),
    });
    payload = await response.json();
  } catch (error) {
    log('err', `request failed: ${error.message}`);
    return;
  }

  if (payload.error) {
    log('err', `${payload.error.code}: ${payload.error.message}`);
    return;
  }

  for (const messages of a2uiParts(payload.result)) {
    log('in', messages.map(m => Object.keys(m).find(k => k !== 'version')).join(', '));
    processor.processMessages(messages);
  }

  const text = textParts(payload.result).join(' ').trim();
  if (text) {
    log('in', `text "${text}"`);
  }
}

function dataModelMetadata() {
  const model = processor.getClientDataModel('v0.9.1');
  return model ? { a2uiClientDataModel: model } : {};
}

/** The renderer reports an action; the wire format needs a timestamp alongside it. */
function toWire(action) {
  return {
    name: action.name,
    surfaceId: action.surfaceId,
    sourceComponentId: action.sourceComponentId,
    timestamp: action.timestamp ?? new Date().toISOString(),
    context: action.context ?? {},
  };
}

/** Walks a JSON-RPC result for message parts, whatever shape the response took. */
function* allParts(node) {
  if (Array.isArray(node)) {
    for (const item of node) {
      yield* allParts(item);
    }
    return;
  }

  if (node && typeof node === 'object') {
    if (Array.isArray(node.parts)) {
      yield* node.parts;
    }
    for (const value of Object.values(node)) {
      yield* allParts(value);
    }
  }
}

function* a2uiParts(result) {
  for (const part of allParts(result)) {
    if (part?.metadata?.mimeType === A2UI_MIME && Array.isArray(part.data)) {
      yield part.data;
    }
  }
}

function textParts(result) {
  return [...allParts(result)].filter(p => typeof p?.text === 'string' && p.text).map(p => p.text);
}

function log(kind, text) {
  const item = document.createElement('li');
  item.className = kind;
  item.textContent = text;
  entries.prepend(item);

  while (entries.children.length > 40) {
    entries.lastElementChild.remove();
  }
}

// Opening the page is the first turn: no action yet, so the agent shows the menu.
send({ text: 'hello' });
