const functions = require("firebase-functions");
const admin = require("firebase-admin");
const express = require("express");
const cors = require("cors");

const app = express();
admin.initializeApp();
const db = admin.firestore();

app.use(cors({origin: true}));
app.use(express.json());

// Registrar
app.post("/registrar", async (req, res) => {
  const {email, senha, usuario} = req.body;
  if (!email || !senha || !usuario) {
    return res.status(400).send({error: "Campos obrigatórios."});
  }

  try {
    const userRecord = await admin.auth().createUser({
      email,
      password: senha,
    });

    await db.collection("usuarios").doc(userRecord.uid).set({
      email,
      usuario,
      senha,
      posicao: {x: 0, y: 0, z: 0},
    });

    return res.status(201).send({uid: userRecord.uid, usuario});
  } catch (err) {
    if (err.code === "auth/email-already-exists") {
      return res.status(400).send({error: "Este e-mail já está em uso."});
    }

    return res.status(400).send({error: err.message});
  }
});

// Pega as informações do usuário
app.get("/usuario/:uid", async (req, res) => {
  const uid = req.params.uid;

  try {
    const doc = await db.collection("usuarios").doc(uid).get();

    if (!doc.exists) {
      return res.status(404).send({error: "Usuário não encontrado."});
    }

    return res.status(200).send(doc.data());
  } catch (error) {
    return res.status(500).send({error: error.message});
  }
});

// Login
app.post("/login", async (req, res) => {
  const {email, senha} = req.body;
  if (!email || !senha) {
    return res.status(400).send({error: "Campos obrigatórios."});
  }

  try {
    const user = await admin.auth().getUserByEmail(email);
    const doc = await db.collection("usuarios").doc(user.uid).get();

    if (!doc.exists || doc.data().senha !== senha) {
      return res.status(401).send({error: "Email ou senha inválidos."});
    }

    const dados = doc.data();

    return res.send({
      uid: user.uid,
      usuario: dados.usuario,
      posicao: dados.posicao || {x: 0, y: 0, z: 0},
    });
  } catch (err) {
    return res.status(500).send({error: err.message});
  }
});

// Salvar progresso
app.post("/salvarProgresso", async (req, res) => {
  const {uid, x, y, z} = req.body;
  if (!uid || x === undefined || y === undefined || z === undefined) {
    return res.status(400).send({error: "UID e posição obrigatórios."});
  }

  try {
    await db.collection("usuarios").doc(uid).update({
      posicao: {x, y, z},
    });

    return res.send({status: "Progresso salvo com sucesso"});
  } catch (err) {
    return res.status(500).send({error: err.message});
  }
});

// Carregar progresso
app.get("/carregarProgresso/:uid", async (req, res) => {
  try {
    const doc = await db.collection("usuarios").doc(req.params.uid).get();

    if (!doc.exists) {
      return res.status(404).send({error: "Usuário não encontrado."});
    }

    return res.send({
      posicao: doc.data().posicao || {x: 0, y: 0, z: 0},
    });
  } catch (err) {
    return res.status(500).send({error: err.message});
  }
});

exports.api = functions.https.onRequest(app);
