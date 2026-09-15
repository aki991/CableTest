// Zvučni signal uz rezultat.
//
// U desktop verziji zvuk pušta Windows (SystemSounds); ovde ga pušta pretraživač, jer server
// nema zvučnik ni operatera pored sebe. Bez fajlova — ton se sintetiše, pa nema šta da nedostaje
// na proizvodnoj mašini.
//
// PASS je kratak i vedar (dva tona naviše), FAIL je oštar i duži (dva tona naniže). Razlika mora
// da se čuje i kroz buku u pogonu.

let context = null;

function audio() {
    if (context === null) {
        context = new (window.AudioContext || window.webkitAudioContext)();
    }

    // Pretraživač obustavi zvuk dok korisnik ne klikne po stranici; prvo sviranje ga vraća.
    if (context.state === "suspended") {
        context.resume();
    }

    return context;
}

function ton(ctx, frekvencija, pocetak, trajanje, jacina) {
    const oscilator = ctx.createOscillator();
    const pojacanje = ctx.createGain();

    oscilator.type = "square";
    oscilator.frequency.value = frekvencija;

    // Blagi uspon i pad, da ton ne pukne na zvučniku.
    pojacanje.gain.setValueAtTime(0, pocetak);
    pojacanje.gain.linearRampToValueAtTime(jacina, pocetak + 0.01);
    pojacanje.gain.setValueAtTime(jacina, pocetak + trajanje - 0.03);
    pojacanje.gain.linearRampToValueAtTime(0, pocetak + trajanje);

    oscilator.connect(pojacanje).connect(ctx.destination);
    oscilator.start(pocetak);
    oscilator.stop(pocetak + trajanje);
}

export function playPass() {
    const ctx = audio();
    const sada = ctx.currentTime;

    ton(ctx, 880, sada, 0.12, 0.18);
    ton(ctx, 1320, sada + 0.13, 0.16, 0.18);
}

export function playFail() {
    const ctx = audio();
    const sada = ctx.currentTime;

    ton(ctx, 300, sada, 0.22, 0.22);
    ton(ctx, 180, sada + 0.24, 0.40, 0.22);
}
