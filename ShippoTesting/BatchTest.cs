﻿using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Shippo;

namespace ShippoTesting
{
    [TestFixture ()]
    public class BatchTest : ShippoTest
    {
        [Test ()]
        public void TestValidCreate ()
        {
            Batch testBatch = getDefaultObject ();
            Assert.AreEqual ("VALIDATING", testBatch.ObjectStatus);
        }

        [Test ()]
        [ExpectedException (typeof (ShippoException))]
        public void TestInvalidCreate ()
        {
            getAPIResource ().CreateBatch ("invalid_carrier_account", "invalid_servicelevel_token", "", "",
                                           new List<Batch.BatchShipment>());
        }

        [Test ()]
        public void TestValidRetrieve ()
        {
            Batch batch = getDefaultObject ();
            Batch retrieve = getAPIResource ().RetrieveBatch (batch.ObjectId);
            Assert.AreEqual (batch.ObjectId, retrieve.ObjectId);
            Assert.AreEqual (batch.ObjectCreated, retrieve.ObjectCreated);
        }

        [Test ()]
        [ExpectedException (typeof (ShippoException))]
        public void TestInvalidRetrieve ()
        {
            getAPIResource ().RetrieveBatch ("INVALID_ID");
        }

        [Test ()]
        public void TestValidAddShipmentToBatch ()
        {
            Batch batch = getDefaultObject ();
            Assert.AreEqual (batch.ObjectStatus, "VALIDATING");

            List<Hashtable> shipments = new List<Hashtable> ();
            Hashtable shipmentTable = new Hashtable ();
            Shipment shipment = ShipmentTest.getDefaultObject ();
            shipmentTable.Add ("shipment", shipment.ObjectId);
            shipments.Add (shipmentTable);

            Batch retrieve = getValidBatch (batch.ObjectId);
            Batch newBatch = getAPIResource ().AddShipmentsToBatch (retrieve.ObjectId, shipments);

            Assert.AreEqual (retrieve.BatchShipments.Count + shipments.Count, newBatch.BatchShipments.Count);
        }

        [Test ()]
        [ExpectedException (typeof (ShippoException))]
        public void TestInvalidAddShipmentToBatch ()
        {
            List<Hashtable> shipments = new List<Hashtable> ();
            Hashtable shipmentTable = new Hashtable ();
            shipmentTable.Add ("shipment", "123");
            shipments.Add (shipmentTable);
            getAPIResource ().AddShipmentsToBatch ("INVALID_ID", shipments);
        }

        [Test ()]
        public void TestValidRemoveShipmentsFromBatch ()
        {
            Batch batch = getDefaultObject ();
            Assert.AreEqual (batch.ObjectStatus, "VALIDATING");

            List<Hashtable> shipments = new List<Hashtable> ();
            Hashtable shipmentTable = new Hashtable ();
            Shipment shipment = ShipmentTest.getDefaultObject ();
            shipmentTable.Add ("shipment", shipment.ObjectId);
            shipments.Add (shipmentTable);

            Batch retrieve = getValidBatch (batch.ObjectId);
            Batch addBatch = getAPIResource ().AddShipmentsToBatch (retrieve.ObjectId, shipments);

            string removeId = addBatch.BatchShipments.Results [0].ObjectId;
            List<String> shipmentsToRemove = new List<String> ();
            shipmentsToRemove.Add (removeId);

            Batch removeBatch = getAPIResource ().RemoveShipmentsFromBatch (batch.ObjectId, shipmentsToRemove);
            Assert.AreEqual (retrieve.BatchShipments.Count, removeBatch.BatchShipments.Count);
        }

        [Test ()]
        [ExpectedException (typeof (ShippoException))]
        public void TestInvalidRemoveShipmentsFromBatch ()
        {
            List<String> shipments = new List<String> ();
            shipments.Add ("123");
            getAPIResource ().RemoveShipmentsFromBatch ("INVALID_ID", shipments);
        }

        [Test ()]
        public void TestValidPurchase ()
        {
            Batch batch = getDefaultObject ();
            Batch retrieve = getValidBatch (batch.ObjectId);
            Batch purchase = getAPIResource ().PurchaseBatch (retrieve.ObjectId);
            Assert.AreEqual ("PURCHASING", purchase.ObjectStatus);
        }

        [Test ()]
        [ExpectedException (typeof (ShippoException))]
        public void TestInvalidPurchase ()
        {
            getAPIResource ().PurchaseBatch ("INVALID_ID");
        }

        /**
         * Retries up to 10 times to retrieve a batch that has been recently
         * created until the newly created batch is 'VALID' and not 'VALIDATING'.
         */
        public Batch getValidBatch (String id)
        {
            Batch batch;
            int retries = 10;
            for (; retries > 0; retries--) {
                batch = getAPIResource ().RetrieveBatch (id);
                if (batch.ObjectStatus.ToString () == "VALID")
                    return batch;
                System.Threading.Thread.Sleep (1000);
            }
            throw new ShippoException ("Could not retrieve valid Batch", new TimeoutException());
        }

        public static Batch getDefaultObject ()
        {
            // Grab USPS carrier account to get the correct object ID for further testing.
            // This should be changed to be more generic in future versions of this test. In
            // other words, remove the depedence on a USPS carrier account to exist.
            ShippoCollection<CarrierAccount> carrierAccounts = getAPIResource ().AllCarrierAccount ();
            string defaultCarrierAccount = "";
            foreach (CarrierAccount account in carrierAccounts) {
                if (account.Carrier.ToString() == "usps")
                    defaultCarrierAccount = account.ObjectId;
            }

            Batch.Address addressFrom = Batch.Address.createForPurchase ("PURCHASE", "Mr. Hippo", "965 Mission St.", "Ste 201",
                                                             "SF", "CA", "94103", "US", "4151234567", "ship@gmail.com");
            Batch.Address addressTo = Batch.Address.createForPurchase ("PURCHASE", "Mrs. Hippo", "965 Missions St.", "Ste 201",
                                                           "SF", "CA", "94103", "US", "4151234568", "msship@gmail.com");
            Batch.Parcel parcel = Batch.Parcel.createForShipment (5, 5, 5, "in", 2, "oz");
            Batch.Shipment shipment = Batch.Shipment.createForBatch ("PURCHASE", addressFrom, addressTo, parcel);
            Batch.BatchShipment batchShipment = Batch.BatchShipment.createForBatchShipments (defaultCarrierAccount, "usps_priority", shipment);

            List<Batch.BatchShipment> batchShipments = new List<Batch.BatchShipment> ();
            batchShipments.Add (batchShipment);

            return getAPIResource ().CreateBatch (defaultCarrierAccount, "usps_priority", "PDF_4x6", "BATCH #170",
                                                  batchShipments);
        }
    }
}
