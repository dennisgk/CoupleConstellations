const express = require("express");
const app = express();
const mysql = require('mysql');
const fs = require('fs');
const formidable = require('formidable');

const dataDir = "./data";
const tmpDir = "./tmp";
const imageNotFoundFile = "./data/ImageNotFound.jpg";

const DBController = require('./db.js');
const databaseController = new DBController(mysql, fs, dataDir, imageNotFoundFile, tmpDir);

//use json to parse

app.use(express.urlencoded({
    extended: "true"
}));
app.use(express.json());
//keeps public directory open
app.use(express.static('public'));

//tester
app.get("/test", async (req, res) => {
    try {
        res.json("bruh");
    } catch (err) {
        console.log(err.message);
    }
});


app.post("/createGalaxy", async (req, res) => {
    try {
        let onCompletion = function(connectCode, code) {
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": code
            });
        }
        databaseController.createGalaxy(req.body.galaxyName, req.body.galaxySecretCode, req.body.galaxyCreator, onCompletion);

    } catch (err) {
        console.log(err.message);
    }
});

app.post("/doesGalaxyExist", async (req, res) => {
    try {

        let onCompletion = function(connectCode, secretCode, existInt) {
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existInt
            });
        }
        databaseController.galaxyExists(req.body.clientConnectCode, req.body.secretJoinCode, onCompletion);

    } catch (err) {
        console.log(err.message);
    }
});

app.post("/buildGalaxy", async (req, res) => {
    try {

        let onCompletion = function(connectCode, secretCode, galaxyName, galaxyCreator, existInt, constellations) {
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "galaxyName": galaxyName,
                "galaxyCreator": galaxyCreator,
                "exists": existInt,
                "constellations": constellations
            });
        }
        databaseController.buildGalaxy(req.body.clientConnectCode, req.body.secretJoinCode, onCompletion);

    } catch (err) {
        console.log(err.message);
    }
});

app.post("/createConstellation", async (req, res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID, constellationName){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt, //only 0 if they have incorrect credentials
                "constellationUID": constellationUID,
                "constellationName": constellationName
            });
        }
        databaseController.createConstellation(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationName, req.body.points, onCompletion);

    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/getConstellationData", async(req,res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID, constellationName, extraDataType, extraData){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt, //only 0 if they have incorrect credentials
                "constellationUID": constellationUID,
                "constellationName": constellationName,
                "dataType": extraDataType,
                "extraData": extraData
            });
        };
        databaseController.getConstellationData(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, req.body.dataType, onCompletion);

    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/getConstellationImageFromStruct", async(req,res) => {
    try{
        let onCompletion = function(imageServerPath){

            if(fs.existsSync(imageServerPath)){
                res.sendFile(imageServerPath, {root: __dirname});
            }
            else{
                res.sendFile(imageNotFoundFile, {root: __dirname});
            }
        }
        databaseController.getConstellationImageFromStruct(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, req.body.imagePath, onCompletion);
    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/removeConstellation", async(req,res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt,
                "constellationUID": constellationUID
            });
        }
        databaseController.removeConstellation(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, onCompletion);
    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/editConstellation/uploadImageData", async(req, res) => {

    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID, creator, sourceHolderFile){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt,
                "constellationUID": constellationUID,
                "creator": creator,
                "sourceHolderFile": sourceHolderFile
            });
        }
        
        let form = formidable({multiples:false});

        form.maxFileSize = 1024 * 1024 * 1024; //50 mb

        form.parse(req, (err, fields, files) => {
            if(err){
                onCompletion(fields.clientConnectCode, fields.secretJoinCode, 0, fields.constellationUID, fields.creator, "");
                return;
            }

            let flConditions = fields;

            if(Object.keys(fields).length == 0){
                flConditions = req.query;
            }

            databaseController.uploadImageData(flConditions.clientConnectCode, flConditions.secretJoinCode, flConditions.constellationUID, flConditions.creator, onCompletion, files);
        });
    }
    catch(err){
        console.log(err.message);
    }

});

app.post("/editConstellation/deleteImageData", async(req, res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt,
                "constellationUID": constellationUID
            });
        }
        databaseController.deleteImageData(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, req.body.sourceHolderFile, onCompletion);
    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/editConstellation/uploadNoteData", async(req, res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID, creator, noteText, sourceHolderFile){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt,
                "constellationUID": constellationUID,
                "creator": creator,
                "noteText": noteText,
                "sourceHolderFile": sourceHolderFile
            });
        }
        databaseController.uploadNoteData(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, req.body.creator, req.body.noteText, onCompletion);
    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/editConstellation/deleteNoteData", async(req, res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt,
                "constellationUID": constellationUID
            });
        }
        databaseController.deleteNoteData(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, req.body.sourceHolderFile, onCompletion);
    }
    catch(err){
        console.log(err.message);
    }
});

app.post("/editConstellation/editPoints", async(req, res) => {
    try{
        let onCompletion = function(connectCode, secretCode, existsInt, constellationUID, points){
            res.json({
                "clientConnectCode": connectCode,
                "secretJoinCode": secretCode,
                "exists": existsInt,
                "constellationUID": constellationUID,
                "points":points
            });
        }
        databaseController.editPointData(req.body.clientConnectCode, req.body.secretJoinCode, req.body.constellationUID, req.body.points, onCompletion);
    }
    catch(err){
        console.log(err.message);
    }
});


app.listen(5959, ()=>{
    console.log("server has started");
});